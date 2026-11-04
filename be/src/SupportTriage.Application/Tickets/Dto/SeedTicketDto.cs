namespace SupportTriage.Application.Tickets.Dto;

public sealed record SeedTicketDto(
    string Id,
    string TenantId,
    string CustomerEmail,
    string Subject,
    string Body,
    DateTime CreatedAt,
    string? Category,
    string? Priority,
    string? SuggestedReply);
