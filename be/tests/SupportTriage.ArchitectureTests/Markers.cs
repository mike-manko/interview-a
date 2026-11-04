using System.Reflection;

namespace SupportTriage.ArchitectureTests;

// Marker types are resolved by name, so this project needs no edits when it is copied into another service.
internal static class Markers
{
    public static readonly Type DomainEvent = Domain("IDomainEvent");

    public static readonly Type AggregateRoot = Domain("IAggregateRoot`1");

    public static readonly Type EntityBase = Domain("EntityBase`1");

    public static readonly Type ValueObjectBase = Domain("ValueObjectBase`1");

    public static readonly Type ExceptionBase = Domain("ExceptionBase");

    public static readonly Type Repository = Domain("IRepository`2");

    public static readonly Type UnitOfWork = Domain("IUnitOfWork");

    public static readonly Type ErrorMessage = Domain("ErrorMessage");

    public static readonly Type? ApiControllerBase = FindOrNull(ArchitectureAssemblies.Api, "ApiControllerBase");

    // Mediator's abstractions are whatever assembly IDomainEvent's INotification comes from, so the package's assembly name does not matter.
    private static readonly Assembly MediatorAssembly = FindInterface(DomainEvent, "INotification").Assembly;

    public static readonly Type Request = FromMediator("Mediator.IRequest");

    public static readonly Type RequestOfT = FromMediator("Mediator.IRequest`1");

    public static readonly Type RequestHandler = FromMediator("Mediator.IRequestHandler`1");

    public static readonly Type RequestHandlerOfT = FromMediator("Mediator.IRequestHandler`2");

    public static readonly Type NotificationHandler = FromMediator("Mediator.INotificationHandler`1");

    public static readonly Type? EntityTypeConfiguration = Type.GetType("Microsoft.EntityFrameworkCore.IEntityTypeConfiguration`1, Microsoft.EntityFrameworkCore");

    private static Type Domain(string name)
    {
        return FindOrNull(ArchitectureAssemblies.Domain, name)
            ?? throw new TypeLoadException("Marker type " + name + " was not found in " + ArchitectureAssemblies.NameOf(ArchitectureAssemblies.Domain) + ". Adjust Markers to the solution's shared kernel.");
    }

    private static Type? FindOrNull(Assembly assembly, string name)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.Name == name)
                return type;
        }

        return null;
    }

    private static Type FindInterface(Type type, string name)
    {
        foreach (var contract in type.GetInterfaces())
        {
            if (contract.Name == name)
                return contract;
        }

        throw new TypeLoadException(type.FullName + " does not implement " + name + ".");
    }

    private static Type FromMediator(string fullName)
    {
        return MediatorAssembly.GetType(fullName) ?? throw new TypeLoadException("Type " + fullName + " was not found in " + ArchitectureAssemblies.NameOf(MediatorAssembly) + ".");
    }
}
