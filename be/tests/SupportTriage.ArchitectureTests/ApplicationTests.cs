using System.Reflection;

namespace SupportTriage.ArchitectureTests;

// Rules 12-13 (testing.md "Architecture tests"): requests are immutable data; handlers live in Application.
[TestFixture]
public sealed class ApplicationTests
{
    // Justified exceptions: full type name → reason. The list should only shrink.
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.Ordinal);

    [Test]
    public void CommandsAndQueries_AreSealedRecordsWithInitOnlyPropertiesAndNoDomainTypes()
    {
        var offenders = new List<string>();
        foreach (var type in ArchitectureAssemblies.Application.ProductionTypes())
        {
            if (type.IsInterface || type.IsAbstract || !type.IsRequest() || IsAllowed(type))
                continue;

            if (!type.IsSealed)
                offenders.Add(type.Describe() + " is not sealed");

            if (!type.IsRecord())
                offenders.Add(type.Describe() + " is not a record");

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.SetMethod is { IsPublic: true } setter && !setter.IsInitOnly())
                    offenders.Add(property.Describe() + " has a setter that is not init-only");

                if (property.PropertyType.IsDomainModelType())
                    offenders.Add(property.Describe() + " carries an entity or value object");
            }
        }

        AssertNone(offenders, "Commands and queries are sealed records carrying primitives and domain enums");
    }

    [Test]
    public void Handlers_LiveInApplicationAndAreSealed()
    {
        var offenders = new List<string>();
        foreach (var assembly in ArchitectureAssemblies.Production)
        {
            foreach (var type in assembly.ProductionTypes())
            {
                if (type.IsInterface || type.IsAbstract || !type.IsHandler() || IsAllowed(type))
                    continue;

                if (assembly != ArchitectureAssemblies.Application)
                    offenders.Add(type.Describe() + " lives in " + ArchitectureAssemblies.NameOf(assembly));

                if (!type.IsSealed)
                    offenders.Add(type.Describe() + " is not sealed");
            }
        }

        AssertNone(offenders, "Request and notification handlers are sealed classes in the Application layer");
    }

    private static bool IsAllowed(Type type) => Allowed.ContainsKey(type.Describe());

    private static void AssertNone(List<string> offenders, string rule)
    {
        Assert.That(offenders, Is.Empty, rule + ":" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }
}
