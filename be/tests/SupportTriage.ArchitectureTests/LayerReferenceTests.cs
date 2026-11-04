using System.Reflection;

namespace SupportTriage.ArchitectureTests;

// Rules 1-3 (testing.md "Architecture tests"): references point inward only; the domain knows no store or transport.
[TestFixture]
public sealed class LayerReferenceTests
{
    private static readonly string[] StoreAndTransportPrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Azure.Cosmos",
        "Microsoft.Data.SqlClient",
        "MassTransit",
        "Azure.Messaging",
        "RabbitMQ",
        "Confluent.Kafka",
        "NServiceBus",
        "Rebus",
    ];

    private static readonly string[] StorePrefixes = ["Microsoft.EntityFrameworkCore", "Microsoft.Azure.Cosmos", "Microsoft.Data.SqlClient"];

    private static readonly string[] BaseClassLibraryPrefixes = ["System", "netstandard", "mscorlib", "Microsoft.CSharp"];

    [Test]
    public void Domain_ReferencesNoOuterLayerAndNoStoreOrTransport()
    {
        var forbidden = Names(ArchitectureAssemblies.Application, ArchitectureAssemblies.Persistence, ArchitectureAssemblies.Infrastructure, ArchitectureAssemblies.Api);

        AssertNoReference(ArchitectureAssemblies.Domain, forbidden, StoreAndTransportPrefixes);
    }

    [Test]
    public void Application_ReferencesNoOuterLayerAndNoStore()
    {
        var forbidden = Names(ArchitectureAssemblies.Persistence, ArchitectureAssemblies.Infrastructure, ArchitectureAssemblies.Api);

        AssertNoReference(ArchitectureAssemblies.Application, forbidden, StorePrefixes);
    }

    [Test]
    public void PersistenceAndInfrastructure_DoNotReferenceApi()
    {
        var forbidden = Names(ArchitectureAssemblies.Api);

        var offenders = Offenders(ArchitectureAssemblies.Persistence, forbidden, []);
        offenders.AddRange(Offenders(ArchitectureAssemblies.Infrastructure, forbidden, []));

        Assert.That(offenders, Is.Empty, "Persistence and Infrastructure reference the host: " + string.Join(", ", offenders));
    }

    [Test]
    public void Contracts_ReferencesOnlyTheBaseClassLibrary()
    {
        var contracts = ArchitectureAssemblies.Contracts;
        if (contracts is null)
            return;

        var offenders = new List<string>();
        foreach (var reference in contracts.GetReferencedAssemblies())
        {
            var name = reference.Name ?? string.Empty;
            if (!StartsWithAny(name, BaseClassLibraryPrefixes))
                offenders.Add(name);
        }

        Assert.That(offenders, Is.Empty, "Contracts must reference nothing outside the base class library: " + string.Join(", ", offenders));
    }

    private static HashSet<string> Names(params Assembly[] assemblies)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var assembly in assemblies)
            names.Add(ArchitectureAssemblies.NameOf(assembly));

        return names;
    }

    private static void AssertNoReference(Assembly assembly, HashSet<string> forbiddenNames, string[] forbiddenPrefixes)
    {
        var offenders = Offenders(assembly, forbiddenNames, forbiddenPrefixes);

        Assert.That(offenders, Is.Empty, ArchitectureAssemblies.NameOf(assembly) + " references forbidden assemblies: " + string.Join(", ", offenders));
    }

    private static List<string> Offenders(Assembly assembly, HashSet<string> forbiddenNames, string[] forbiddenPrefixes)
    {
        var offenders = new List<string>();
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            var name = reference.Name ?? string.Empty;
            if (forbiddenNames.Contains(name) || StartsWithAny(name, forbiddenPrefixes))
                offenders.Add(ArchitectureAssemblies.NameOf(assembly) + " -> " + name);
        }

        return offenders;
    }

    private static bool StartsWithAny(string name, string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
