using System.Globalization;
using System.Text;
using Microsoft.Azure.Cosmos;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Persistence.QuerySources;

// Builds the ticket list query from fixed text and parameters only; filter values never become part of the query
// text. Values are added in enum order, so the same filter always gives the same query, which a continuation token
// requires.
internal static class TicketListQueryBuilder
{
    private const string Projection =
        "SELECT c.id, c.customerEmail, c.subject, c.body, c.status, c.category, c.priority, c.suggestedReply, c.createdAt, c.classifiedAt, " +
        "c.failedTriageAttempts, c.triageFailureReason FROM c WHERE c.isDeleted = false";

    private const string OrderBy = " ORDER BY c.createdAt DESC";

    public static QueryDefinition Build(TicketListFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var text = new StringBuilder(Projection);
        var parameters = new List<KeyValuePair<string, string>>();

        if (!filter.IncludeHidden)
        {
            text.Append(" AND c.status != @hidden");
            parameters.Add(new KeyValuePair<string, string>("@hidden", nameof(TicketStatus.Failed)));
        }

        AppendIn(text, parameters, "c.category", "@category", Selected(filter.Categories));
        AppendIn(text, parameters, "c.priority", "@priority", Selected(filter.Priorities));
        text.Append(OrderBy);

        var query = new QueryDefinition(text.ToString());
        foreach (var parameter in parameters)
            query = query.WithParameter(parameter.Key, parameter.Value);

        return query;
    }

    private static List<string> Selected<TEnum>(IReadOnlySet<TEnum> selected)
        where TEnum : struct, Enum
    {
        var names = new List<string>(selected.Count);
        foreach (var value in Enum.GetValues<TEnum>())
        {
            if (selected.Contains(value))
                names.Add(value.ToString());
        }

        return names;
    }

    private static void AppendIn(StringBuilder text, List<KeyValuePair<string, string>> parameters, string field, string parameterPrefix, List<string> values)
    {
        if (values.Count == 0)
            return;

        text.Append(" AND ").Append(field).Append(" IN (");
        for (var i = 0; i < values.Count; i++)
        {
            var name = parameterPrefix + i.ToString(CultureInfo.InvariantCulture);
            if (i > 0)
                text.Append(", ");

            text.Append(name);
            parameters.Add(new KeyValuePair<string, string>(name, values[i]));
        }

        text.Append(')');
    }
}
