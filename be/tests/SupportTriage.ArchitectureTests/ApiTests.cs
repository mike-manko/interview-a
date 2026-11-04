using System.Reflection;

namespace SupportTriage.ArchitectureTests;

// Rule 15 (testing.md "Architecture tests"), checked on signatures; inspecting method bodies needs an IL library.
[TestFixture]
public sealed class ApiTests
{
    // Justified exceptions: full type name → reason. The list should only shrink.
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.Ordinal);

    [Test]
    public void Controllers_DeriveFromApiControllerBaseAndAreSealed()
    {
        var controllerBase = Markers.ApiControllerBase;
        if (controllerBase is null)
        {
            Assert.Fail("ApiControllerBase was not found in " + ArchitectureAssemblies.NameOf(ArchitectureAssemblies.Api));
            return;
        }

        var offenders = new List<string>();
        foreach (var controller in Controllers())
        {
            if (!controllerBase.IsAssignableFrom(controller))
                offenders.Add(controller.Describe() + " does not derive from ApiControllerBase");

            if (!controller.IsSealed)
                offenders.Add(controller.Describe() + " is not sealed");
        }

        AssertNone(offenders, "Controllers are sealed and derive from ApiControllerBase");
    }

    [Test]
    public void Controllers_ReferenceNoDomainModelTypesAndNoOuterLayers()
    {
        var offenders = new List<string>();
        foreach (var controller in Controllers())
        {
            foreach (var constructor in controller.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                    Check(offenders, constructor, parameter.ParameterType);
            }

            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName)
                    continue;

                Check(offenders, method, method.ReturnType);
                foreach (var parameter in method.GetParameters())
                    Check(offenders, method, parameter.ParameterType);
            }

            foreach (var property in controller.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                Check(offenders, property, property.PropertyType);
        }

        AssertNone(offenders, "Controllers bind requests and send commands or queries; they never see entities, value objects, Persistence or Infrastructure");
    }

    private static void Check(List<string> offenders, MemberInfo member, Type type)
    {
        if (type.IsDomainModelType())
            offenders.Add(member.Describe() + " uses " + type.Name);

        if (type.UsesAssembly(ArchitectureAssemblies.Persistence) || type.UsesAssembly(ArchitectureAssemblies.Infrastructure))
            offenders.Add(member.Describe() + " uses " + type.Name + " from an outer layer");
    }

    private static List<Type> Controllers()
    {
        var controllers = new List<Type>();
        foreach (var type in ArchitectureAssemblies.Api.ProductionTypes())
        {
            if (!type.IsAbstract && type.BareName().EndsWith("Controller", StringComparison.Ordinal) && !Allowed.ContainsKey(type.Describe()))
                controllers.Add(type);
        }

        return controllers;
    }

    private static void AssertNone(List<string> offenders, string rule)
    {
        Assert.That(offenders, Is.Empty, rule + ":" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }
}
