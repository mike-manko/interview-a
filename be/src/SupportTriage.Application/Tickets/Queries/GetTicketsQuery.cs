using Mediator;
using SupportTriage.Application.Common.Dto;
using SupportTriage.Application.Tickets.Constants;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Services;

namespace SupportTriage.Application.Tickets.Queries;

// The current tenant's tickets with their classification, newest first. Hidden tickets (failed triage) are listed
// only when IncludeHidden is true, so an operator can find and classify them. Categories and Priorities keep only
// tickets with one of the given values; they accept the exact allowed names, like classification does.
public sealed record GetTicketsQuery(
    int? PageSize,
    string? ContinuationToken,
    bool IncludeHidden,
    IReadOnlyList<string>? Categories,
    IReadOnlyList<string>? Priorities) : IRequest<ContinuationPageDto<TicketDto>>;

public sealed class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, ContinuationPageDto<TicketDto>>
{
    private static readonly string AllowedCategories = string.Join(", ", TriageValueParser.CategoryNames);

    private static readonly string AllowedPriorities = string.Join(", ", TriageValueParser.PriorityNames);

    private readonly ITicketQuerySource _ticketQuerySource;

    public GetTicketsQueryHandler(ITicketQuerySource ticketQuerySource)
    {
        _ticketQuerySource = ticketQuerySource ?? throw new ArgumentNullException(nameof(ticketQuerySource));
    }

    public async ValueTask<ContinuationPageDto<TicketDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        var filter = BuildFilter(request);

        return await _ticketQuerySource.GetTicketsAsync(TicketPaging.Clamp(request.PageSize), request.ContinuationToken, filter, cancellationToken);
    }

    // Every invalid value is reported at once.
    private static TicketListFilter BuildFilter(GetTicketsQuery request)
    {
        List<ErrorMessage>? errors = null;

        var categories = new HashSet<TicketCategory>();
        if (request.Categories is not null)
        {
            foreach (var value in request.Categories)
            {
                if (TriageValueParser.TryParseCategory(value, out var category))
                    categories.Add(category);
                else
                    (errors ??= []).Add(TicketApplicationErrorMessages.CategoryFilterIsNotAllowed.WithMessageFormat(value, AllowedCategories).WithKey("category"));
            }
        }

        var priorities = new HashSet<TicketPriority>();
        if (request.Priorities is not null)
        {
            foreach (var value in request.Priorities)
            {
                if (TriageValueParser.TryParsePriority(value, out var priority))
                    priorities.Add(priority);
                else
                    (errors ??= []).Add(TicketApplicationErrorMessages.PriorityFilterIsNotAllowed.WithMessageFormat(value, AllowedPriorities).WithKey("priority"));
            }
        }

        if (errors is not null)
            throw new InvalidTicketFilterException(errors);

        return new TicketListFilter(request.IncludeHidden, categories, priorities);
    }
}
