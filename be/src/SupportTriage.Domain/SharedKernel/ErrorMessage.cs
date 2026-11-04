using System.Globalization;

namespace SupportTriage.Domain.SharedKernel;

public sealed record ErrorMessage
{
    public string Message { get; }

    public int Code { get; }

    public ErrorType Type { get; }

    public string? UserFriendlyMessage { get; }

    public string? Key { get; }

    public ErrorMessage(string message, int code, ErrorType type = ErrorType.Validation, string? userFriendlyMessage = null, string? key = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Message = message;
        Code = code;
        Type = type;
        UserFriendlyMessage = userFriendlyMessage;
        Key = key;
    }

    public ErrorMessage WithMessageFormat(params object?[] args)
    {
        var message = string.Format(CultureInfo.InvariantCulture, Message, args);
        var userFriendlyMessage = UserFriendlyMessage is null ? null : string.Format(CultureInfo.InvariantCulture, UserFriendlyMessage, args);

        return new ErrorMessage(message, Code, Type, userFriendlyMessage, Key);
    }

    public ErrorMessage WithKey(object key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return new ErrorMessage(Message, Code, Type, UserFriendlyMessage, Convert.ToString(key, CultureInfo.InvariantCulture));
    }
}
