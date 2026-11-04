using System.ComponentModel.DataAnnotations;

namespace SupportTriage.Infrastructure.OpenAi.Settings;

internal sealed class OpenAiSettings
{
    public const string SectionName = "OpenAi";

    [Required]
    public string ApiKey { get; set; } = null!;

    // Null means the public OpenAI endpoint.
    public Uri? Endpoint { get; set; }

    [Required]
    public string ClassificationModel { get; set; } = null!;

    // Null leaves the model default; set it to null for models that reject a temperature.
    [Range(0d, 2d)]
    public float? ClassificationTemperature { get; set; } = 0f;

    [Range(16, 4_096)]
    public int ClassificationMaxOutputTokens { get; set; } = 200;

    // Model for the prompt-injection screening call; null or empty uses ClassificationModel.
    public string? ScreeningModel { get; set; }

    [Range(16, 4_096)]
    public int ScreeningMaxOutputTokens { get; set; } = 100;

    // Retries after the first attempt, with exponential backoff and jitter, on 408, 429, 5xx and network failures.
    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 4;

    [Range(typeof(TimeSpan), "00:00:00.001", "00:01:00")]
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(30);

    [Range(typeof(TimeSpan), "00:00:01", "00:30:00")]
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
