using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tests.Mocks;

internal sealed class FixedIdGenerator : IIdGenerator<string>
{
    private readonly string _id;

    public FixedIdGenerator(string id)
    {
        _id = id;
    }

    public string NewId() => _id;

    public ValueTask<string> NewIdAsync(CancellationToken cancellationToken = default) => new(_id);
}
