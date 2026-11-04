using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SupportTriage.ArchitectureTests;

internal static class TypeExtensions
{
    private const string InitOnlyModifier = "System.Runtime.CompilerServices.IsExternalInit";

    // Compiler-generated types, migrations and Mediator's generated code are not subject to the rules.
    public static IEnumerable<Type> ProductionTypes(this Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsGenerated())
                continue;

            var ns = type.Namespace ?? string.Empty;
            if (ns.Contains(".Migrations", StringComparison.Ordinal) || ns.StartsWith("Mediator", StringComparison.Ordinal))
                continue;

            yield return type;
        }
    }

    public static bool IsGenerated(this Type type)
    {
        if (type.Name.Contains('<', StringComparison.Ordinal) || type.IsDefined(typeof(CompilerGeneratedAttribute), false) || type.IsDefined(typeof(GeneratedCodeAttribute), false))
            return true;

        return type.DeclaringType is not null && type.DeclaringType.IsGenerated();
    }

    public static bool DerivesFromOpenGeneric(this Type type, Type openGeneric)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGeneric)
                return true;
        }

        return false;
    }

    public static bool ImplementsOpenGeneric(this Type type, Type openGeneric)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric)
            return true;

        foreach (var contract in type.GetInterfaces())
        {
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == openGeneric)
                return true;
        }

        return false;
    }

    public static bool Implements(this Type type, Type contract) => contract.IsAssignableFrom(type) && type != contract;

    public static bool IsEntity(this Type type) => type.DerivesFromOpenGeneric(Markers.EntityBase);

    public static bool IsValueObject(this Type type) => type.DerivesFromOpenGeneric(Markers.ValueObjectBase);

    // True for entities and value objects, also when they hide inside arrays, Task<>, collections or other generics.
    public static bool IsDomainModelType(this Type type)
    {
        if (type.IsByRef || type.IsArray || type.IsPointer)
            return type.GetElementType()!.IsDomainModelType();

        if (type.IsEntity() || type.IsValueObject())
            return true;

        if (!type.IsGenericType)
            return false;

        foreach (var argument in type.GetGenericArguments())
        {
            if (argument.IsDomainModelType())
                return true;
        }

        return false;
    }

    public static bool UsesAssembly(this Type type, Assembly assembly)
    {
        if (type.IsByRef || type.IsArray || type.IsPointer)
            return type.GetElementType()!.UsesAssembly(assembly);

        if (type.Assembly == assembly)
            return true;

        if (!type.IsGenericType)
            return false;

        foreach (var argument in type.GetGenericArguments())
        {
            if (argument.UsesAssembly(assembly))
                return true;
        }

        return false;
    }

    public static bool IsRecord(this Type type) => type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;

    public static bool IsInitOnly(this MethodInfo setter)
    {
        foreach (var modifier in setter.ReturnParameter.GetRequiredCustomModifiers())
        {
            if (modifier.FullName == InitOnlyModifier)
                return true;
        }

        return false;
    }

    public static bool ReturnsOrContains(this Type type, Type openGeneric)
    {
        if (type == openGeneric || (type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric))
            return true;

        if (!type.IsGenericType)
            return false;

        foreach (var argument in type.GetGenericArguments())
        {
            if (argument.ReturnsOrContains(openGeneric))
                return true;
        }

        return false;
    }

    public static bool IsRepositoryLike(this Type type)
    {
        return type == Markers.UnitOfWork || type.BareName().EndsWith("Repository", StringComparison.Ordinal) || type.ImplementsOpenGeneric(Markers.Repository);
    }

    public static bool IsRequest(this Type type) => type.Implements(Markers.Request) || type.ImplementsOpenGeneric(Markers.RequestOfT);

    public static bool IsHandler(this Type type)
    {
        return type.ImplementsOpenGeneric(Markers.RequestHandler) || type.ImplementsOpenGeneric(Markers.RequestHandlerOfT) || type.ImplementsOpenGeneric(Markers.NotificationHandler);
    }

    public static bool HasPrivateParameterlessConstructor(this Type type)
    {
        var constructor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes);

        return constructor is { IsPrivate: true };
    }

    // The type name without the generic arity suffix ("IRepository`2" → "IRepository").
    public static string BareName(this Type type)
    {
        var index = type.Name.IndexOf('`', StringComparison.Ordinal);

        return index < 0 ? type.Name : type.Name[..index];
    }

    public static bool EndsWithAny(this string name, params string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static string Describe(this MemberInfo member) => (member.DeclaringType?.FullName ?? "?") + "." + member.Name;

    public static string Describe(this Type type) => type.FullName ?? type.Name;
}
