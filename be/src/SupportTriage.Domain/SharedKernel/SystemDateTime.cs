namespace SupportTriage.Domain.SharedKernel;

// The live clock, registered in AddDomain. The only place in the model that reads the system time.
internal sealed class SystemDateTime : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly UtcDate => DateOnly.FromDateTime(DateTime.UtcNow);
}
