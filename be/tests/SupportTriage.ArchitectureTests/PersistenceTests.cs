namespace SupportTriage.ArchitectureTests;

// Rule 14 (testing.md "Architecture tests"): Persistence exposes interfaces, never implementations.
[TestFixture]
public sealed class PersistenceTests
{
    // Justified exceptions: full type name → reason. The list should only shrink.
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.Ordinal);

    [Test]
    public void RepositoriesQuerySourcesAndConfigurations_AreInternal()
    {
        var offenders = new List<string>();
        foreach (var type in ArchitectureAssemblies.Persistence.ProductionTypes())
        {
            if (type.IsInterface || !IsImplementation(type) || Allowed.ContainsKey(type.Describe()))
                continue;

            if (type.IsPublic || type.IsNestedPublic)
                offenders.Add(type.Describe());
        }

        Assert.That(offenders, Is.Empty, "Repository, query-source and EF configuration implementations are internal:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static bool IsImplementation(Type type)
    {
        if (type.BareName().EndsWithAny("Repository", "RepositoryBase", "QuerySource", "QuerySourceBase") || type.ImplementsOpenGeneric(Markers.Repository))
            return true;

        return Markers.EntityTypeConfiguration is not null && type.ImplementsOpenGeneric(Markers.EntityTypeConfiguration);
    }
}
