using System.Globalization;

namespace SupportTriage.Domain.SharedKernel;

public abstract record ValueObjectBase<TValue>
    where TValue : notnull
{
    public TValue Value { get; protected init; } = default!;

    protected ValueObjectBase()
    {
    }

    public static implicit operator TValue(ValueObjectBase<TValue> valueObject)
    {
        ArgumentNullException.ThrowIfNull(valueObject);

        return valueObject.Value;
    }

    public TValue ToValue() => Value;

    public override string ToString() => Convert.ToString(Value, CultureInfo.InvariantCulture) ?? string.Empty;
}
