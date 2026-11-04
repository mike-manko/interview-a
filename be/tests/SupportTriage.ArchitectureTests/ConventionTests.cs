using System.Reflection;
using System.Runtime.CompilerServices;

namespace SupportTriage.ArchitectureTests;

// Rules 16-18 (testing.md "Architecture tests"): sealed by default, role suffixes, async naming and cancellation.
[TestFixture]
public sealed class ConventionTests
{
    // Justified exceptions: full type name → reason. The list should only shrink.
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.Ordinal);

    // Mediator's Handle and the bus's Consume are framework contracts; everything else returning a task ends with Async.
    private static readonly string[] AsyncNameExceptions = ["Handle", "Consume"];

    [Test]
    public void ClassesWithoutSubclasses_AreSealed()
    {
        var baseTypes = new HashSet<Type>();
        var candidates = new List<Type>();
        foreach (var assembly in ArchitectureAssemblies.Production)
        {
            foreach (var type in assembly.ProductionTypes())
            {
                var baseType = type.BaseType;
                if (baseType is not null)
                    baseTypes.Add(baseType.IsGenericType ? baseType.GetGenericTypeDefinition() : baseType);

                if (type.IsClass && !type.IsAbstract && !type.IsSealed)
                    candidates.Add(type);
            }
        }

        var offenders = new List<string>();
        foreach (var candidate in candidates)
        {
            if (baseTypes.Contains(candidate) || candidate.Name == "Program" || IsAllowed(candidate))
                continue;

            offenders.Add(candidate.Describe());
        }

        AssertNone(offenders, "Classes not designed for inheritance are sealed");
    }

    [Test]
    public void TypeNames_CarryTheirRoleSuffixAndInterfacesStartWithI()
    {
        var offenders = new List<string>();
        foreach (var assembly in ArchitectureAssemblies.Production)
        {
            foreach (var type in assembly.ProductionTypes())
            {
                if (IsAllowed(type))
                    continue;

                var name = type.BareName();
                if (type.IsInterface && !(name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1])))
                    offenders.Add(type.Describe() + " should start with I");

                Expect(offenders, type, name, type.IsRequest(), "Command", "Query");
                Expect(offenders, type, name, type.ImplementsOpenGeneric(Markers.RequestHandler) || type.ImplementsOpenGeneric(Markers.RequestHandlerOfT), "CommandHandler", "QueryHandler");
                Expect(offenders, type, name, HandlesDomainEvent(type), "DomainEventHandler");
                Expect(offenders, type, name, type.Implements(Markers.DomainEvent), "DomainEvent");
                Expect(offenders, type, name, Markers.ExceptionBase.IsAssignableFrom(type) && type != Markers.ExceptionBase, "Exception");
                Expect(offenders, type, name, type.ImplementsOpenGeneric(Markers.Repository), "Repository", "RepositoryBase");
                Expect(offenders, type, name, IsErrorCatalog(type), "ErrorMessages");
                if (Markers.EntityTypeConfiguration is not null)
                    Expect(offenders, type, name, type.ImplementsOpenGeneric(Markers.EntityTypeConfiguration), "Configuration");
            }
        }

        AssertNone(offenders, "Type names state their role");
    }

    [Test]
    public void AsyncMethods_EndWithAsyncTakeTheTokenLastAndNeverReturnVoid()
    {
        var offenders = new List<string>();
        foreach (var assembly in ArchitectureAssemblies.Production)
        {
            foreach (var type in assembly.ProductionTypes())
            {
                if (IsAllowed(type))
                    continue;

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (method.IsSpecialName || method.Name.Contains('<', StringComparison.Ordinal) || method.IsDefined(typeof(CompilerGeneratedAttribute), false))
                        continue;

                    if (method.ReturnType == typeof(void) && method.IsDefined(typeof(AsyncStateMachineAttribute), false))
                    {
                        offenders.Add(method.Describe() + " is async void");
                        continue;
                    }

                    if (!ReturnsTask(method.ReturnType))
                        continue;

                    if (!method.Name.EndsWith("Async", StringComparison.Ordinal) && !IsFrameworkContract(type, method))
                        offenders.Add(method.Describe() + " returns a task but does not end with Async");

                    var parameters = method.GetParameters();
                    for (var i = 0; i < parameters.Length - 1; i++)
                    {
                        if (parameters[i].ParameterType == typeof(CancellationToken))
                            offenders.Add(method.Describe() + " must take the CancellationToken last");
                    }
                }
            }
        }

        AssertNone(offenders, "Async methods end with Async, take the CancellationToken last and never return void");
    }

    private static void Expect(List<string> offenders, Type type, string name, bool applies, params string[] suffixes)
    {
        if (applies && !name.EndsWithAny(suffixes))
            offenders.Add(type.Describe() + " should end with " + string.Join(" or ", suffixes));
    }

    private static bool HandlesDomainEvent(Type type)
    {
        foreach (var contract in type.GetInterfaces())
        {
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == Markers.NotificationHandler && Markers.DomainEvent.IsAssignableFrom(contract.GetGenericArguments()[0]))
                return true;
        }

        return false;
    }

    private static bool IsErrorCatalog(Type type)
    {
        if (!type.IsAbstract || !type.IsSealed)
            return false;

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (field.FieldType == Markers.ErrorMessage)
                return true;
        }

        return false;
    }

    private static bool ReturnsTask(Type type)
    {
        if (type == typeof(Task) || type == typeof(ValueTask))
            return true;

        if (!type.IsGenericType)
            return false;

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(Task<>) || definition == typeof(ValueTask<>);
    }

    // Overrides of framework methods and implementations of framework interfaces (Mediator's Handle, a bus's Consume) keep their names.
    private static bool IsFrameworkContract(Type type, MethodInfo method)
    {
        var simpleName = method.Name[(method.Name.LastIndexOf('.') + 1)..];
        if (Array.IndexOf(AsyncNameExceptions, simpleName) >= 0)
            return true;

        var baseDefinition = method.GetBaseDefinition();
        if (baseDefinition.DeclaringType is { } baseDeclaringType && baseDeclaringType != type && !ArchitectureAssemblies.IsProduction(baseDeclaringType.Assembly))
            return true;

        if (type.IsInterface)
            return false;

        foreach (var contract in type.GetInterfaces())
        {
            if (ArchitectureAssemblies.IsProduction(contract.Assembly))
                continue;

            if (Array.IndexOf(InterfaceTargets(type, contract), method) >= 0)
                return true;
        }

        return false;
    }

    private static MethodInfo[] InterfaceTargets(Type type, Type contract)
    {
        try
        {
            return type.GetInterfaceMap(contract).TargetMethods;
        }
        catch (ArgumentException)
        {
            return [];
        }
        catch (NotSupportedException)
        {
            return [];
        }
    }

    private static bool IsAllowed(Type type) => Allowed.ContainsKey(type.Describe());

    private static void AssertNone(List<string> offenders, string rule)
    {
        Assert.That(offenders, Is.Empty, rule + ":" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }
}
