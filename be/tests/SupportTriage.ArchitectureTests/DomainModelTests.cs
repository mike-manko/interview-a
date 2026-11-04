using System.Collections.ObjectModel;
using System.Reflection;

namespace SupportTriage.ArchitectureTests;

// Rules 4-9 and 11 (testing.md "Architecture tests"): the shape of aggregates, value objects, events and repositories.
[TestFixture]
public sealed class DomainModelTests
{
    // Justified exceptions: full type name → reason. The list should only shrink.
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.Ordinal);

    private static readonly HashSet<Type> MutableCollections =
    [
        typeof(List<>),
        typeof(HashSet<>),
        typeof(ICollection<>),
        typeof(IList<>),
        typeof(ISet<>),
        typeof(Collection<>),
        typeof(ObservableCollection<>),
        typeof(Dictionary<,>),
        typeof(IDictionary<,>),
    ];

    [Test]
    public void Entities_HaveNoPublicSettersAndNoPublicMutableCollections()
    {
        var offenders = new List<string>();
        foreach (var type in ArchitectureAssemblies.Domain.ProductionTypes())
        {
            if (!type.IsEntity() || IsAllowed(type))
                continue;

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (property.SetMethod is { IsPublic: true })
                    offenders.Add(property.Describe() + " has a public setter");

                if (property.PropertyType.IsGenericType && MutableCollections.Contains(property.PropertyType.GetGenericTypeDefinition()))
                    offenders.Add(property.Describe() + " exposes a mutable collection");
            }
        }

        AssertNone(offenders, "Entities keep private setters and expose children as IReadOnlyCollection<T>");
    }

    [Test]
    public void AggregateRoots_HaveAPrivateParameterlessConstructor()
    {
        var offenders = new List<string>();
        foreach (var type in AggregateRoots())
        {
            if (!type.HasPrivateParameterlessConstructor())
                offenders.Add(type.Describe());
        }

        AssertNone(offenders, "Aggregate roots need a private parameterless constructor for EF Core");
    }

    [Test]
    public void AggregateRoots_TakeNoRepositoryOrUnitOfWorkInPublicMethods()
    {
        var offenders = new List<string>();
        foreach (var type in AggregateRoots())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName)
                    continue;

                foreach (var parameter in method.GetParameters())
                {
                    if (parameter.ParameterType.IsRepositoryLike())
                        offenders.Add(method.Describe() + " takes " + parameter.ParameterType.Name);
                }
            }
        }

        AssertNone(offenders, "Aggregates receive value providers and pure policies only, never repositories or the unit of work");
    }

    [Test]
    public void DomainServices_TakeNoRepositoryOrUnitOfWorkInConstructors()
    {
        var offenders = new List<string>();
        foreach (var type in ArchitectureAssemblies.Domain.ProductionTypes())
        {
            if (type.IsInterface || !IsDomainService(type) || IsAllowed(type))
                continue;

            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    if (parameter.ParameterType.IsRepositoryLike())
                        offenders.Add(type.Describe() + " takes " + parameter.ParameterType.Name);
                }
            }
        }

        AssertNone(offenders, "Domain services are stateless and do no I/O; loading belongs to the Application layer");
    }

    [Test]
    public void ValueObjects_AreSealedWithAPrivateParameterlessConstructor()
    {
        var offenders = new List<string>();
        foreach (var type in ArchitectureAssemblies.Domain.ProductionTypes())
        {
            if (type.IsAbstract || !type.IsValueObject() || IsAllowed(type))
                continue;

            if (!type.IsSealed)
                offenders.Add(type.Describe() + " is not sealed");

            if (!type.HasPrivateParameterlessConstructor())
                offenders.Add(type.Describe() + " has no private parameterless constructor");
        }

        AssertNone(offenders, "Value objects are sealed records with a private parameterless constructor for EF Core");
    }

    [Test]
    public void DomainEvents_AreSealedImmutableAndCarryNoEntities()
    {
        var offenders = new List<string>();
        foreach (var type in ArchitectureAssemblies.Domain.ProductionTypes())
        {
            if (type.IsInterface || !type.Implements(Markers.DomainEvent) || IsAllowed(type))
                continue;

            if (!type.IsSealed)
                offenders.Add(type.Describe() + " is not sealed");

            if (type.GetConstructors().Length > 0)
                offenders.Add(type.Describe() + " has a public constructor");

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.SetMethod is { IsPublic: true })
                    offenders.Add(property.Describe() + " has a public setter");

                if (property.PropertyType.IsDomainModelType())
                    offenders.Add(property.Describe() + " carries an entity or value object");
            }
        }

        AssertNone(offenders, "Domain events are sealed, internally constructed, get-only and carry scalar facts");
    }

    [Test]
    public void RepositoryInterfaces_NeverReturnIQueryable()
    {
        var offenders = new List<string>();
        foreach (var type in ArchitectureAssemblies.Domain.ProductionTypes())
        {
            if (!type.IsInterface || !type.IsRepositoryLike() || IsAllowed(type))
                continue;

            foreach (var method in type.GetMethods())
            {
                if (method.ReturnType.ReturnsOrContains(typeof(IQueryable<>)) || method.ReturnType == typeof(IQueryable))
                    offenders.Add(method.Describe());
            }

            foreach (var property in type.GetProperties())
            {
                if (property.PropertyType.ReturnsOrContains(typeof(IQueryable<>)) || property.PropertyType == typeof(IQueryable))
                    offenders.Add(property.Describe());
            }
        }

        AssertNone(offenders, "Repositories return materialized results or IAsyncEnumerable<T>, never IQueryable<T>");
    }

    private static List<Type> AggregateRoots()
    {
        var roots = new List<Type>();
        foreach (var type in ArchitectureAssemblies.Domain.ProductionTypes())
        {
            if (!type.IsInterface && !type.IsAbstract && type.ImplementsOpenGeneric(Markers.AggregateRoot) && !IsAllowed(type))
                roots.Add(type);
        }

        return roots;
    }

    private static bool IsDomainService(Type type)
    {
        return type.BareName().EndsWith("Service", StringComparison.Ordinal) || (type.Namespace ?? string.Empty).Contains(".Services", StringComparison.Ordinal);
    }

    private static bool IsAllowed(Type type) => Allowed.ContainsKey(type.Describe());

    private static void AssertNone(List<string> offenders, string rule)
    {
        Assert.That(offenders, Is.Empty, rule + ":" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }
}
