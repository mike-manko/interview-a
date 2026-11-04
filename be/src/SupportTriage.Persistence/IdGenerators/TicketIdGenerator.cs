using System.Globalization;
using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Persistence.IdGenerators;

// Time-ordered UUIDv7 ids, so ids of new tickets sort roughly by creation time.
internal sealed class TicketIdGenerator : IIdGenerator<string>
{
    public string NewId() => Guid.CreateVersion7().ToString("D", CultureInfo.InvariantCulture);

    public ValueTask<string> NewIdAsync(CancellationToken cancellationToken = default) => new(NewId());
}
