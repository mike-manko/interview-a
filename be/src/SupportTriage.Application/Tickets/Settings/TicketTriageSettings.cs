using System.ComponentModel.DataAnnotations;
using SupportTriage.Domain.TicketAggregate;

namespace SupportTriage.Application.Tickets.Settings;

public sealed class TicketTriageSettings
{
    public const string SectionName = "TicketTriage";

    public bool Enabled { get; set; } = true;

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    [Range(1, 100)]
    public int BatchSize { get; set; } = 20;

    // The model call dominates the time per ticket, so a few tickets run at once.
    [Range(1, 32)]
    public int MaxDegreeOfParallelism { get; set; } = 4;

    // Must be longer than the slowest step (the OpenAI total timeout), or a slow call loses its ticket to another worker.
    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    // Failed attempts after which a ticket is hidden (decision: hide the ticket and keep a note).
    [Range(Ticket.MinTriageAttempts, Ticket.MaxTriageAttempts)]
    public int MaxAttempts { get; set; } = 5;

    // Delay before the next attempt: RetryBaseDelay * 2^(failed attempts - 1), capped at MaxRetryDelay.
    [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00")]
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(30);

    [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00")]
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(30);

    // Delay after the provider rejected our configuration (bad key or model); does not count as an attempt.
    [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00")]
    public TimeSpan ConfigurationErrorRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
}
