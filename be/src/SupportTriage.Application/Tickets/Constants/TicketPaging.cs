namespace SupportTriage.Application.Tickets.Constants;

public static class TicketPaging
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public static int Clamp(int? pageSize) => pageSize switch
    {
        null => DefaultPageSize,
        < 1 => 1,
        > MaxPageSize => MaxPageSize,
        _ => pageSize.Value,
    };
}
