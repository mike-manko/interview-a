using System.Reflection;

namespace SupportTriage.ArchitectureTests;

// Every layer is loaded once, by assembly name derived from this project's name (<Service>.ArchitectureTests → <Service>.<Layer>).
// When the solution does not follow that convention, replace the Load calls with typeof(<LayerDependencyInjection>).Assembly.
internal static class ArchitectureAssemblies
{
    private const string TestSuffix = ".ArchitectureTests";

    private static readonly string ServicePrefix = ResolveServicePrefix();

    public static readonly Assembly Domain = Require("Domain");

    public static readonly Assembly Application = Require("Application");

    public static readonly Assembly Persistence = Require("Persistence");

    public static readonly Assembly Infrastructure = Require("Infrastructure");

    public static readonly Assembly Api = Load("Api") ?? Load("WebApi") ?? Require("Host");

    public static readonly Assembly? Contracts = Load("Contracts");

    public static readonly Assembly[] Production = BuildProduction();

    public static string NameOf(Assembly assembly) => assembly.GetName().Name ?? string.Empty;

    public static bool IsProduction(Assembly assembly) => Array.IndexOf(Production, assembly) >= 0;

    private static string ResolveServicePrefix()
    {
        var testAssemblyName = typeof(ArchitectureAssemblies).Assembly.GetName().Name ?? string.Empty;

        return testAssemblyName.EndsWith(TestSuffix, StringComparison.Ordinal)
            ? testAssemblyName[..^TestSuffix.Length]
            : testAssemblyName;
    }

    private static Assembly Require(string layer)
    {
        return Load(layer) ?? throw new FileNotFoundException("Production assembly " + ServicePrefix + "." + layer + " was not found. Reference it from the architecture test project or adjust ArchitectureAssemblies.");
    }

    private static Assembly? Load(string layer)
    {
        try
        {
            return Assembly.Load(new AssemblyName(ServicePrefix + "." + layer));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private static Assembly[] BuildProduction()
    {
        var assemblies = new List<Assembly> { Domain, Application, Persistence, Infrastructure, Api };
        if (Contracts is not null)
            assemblies.Add(Contracts);

        return assemblies.ToArray();
    }
}
