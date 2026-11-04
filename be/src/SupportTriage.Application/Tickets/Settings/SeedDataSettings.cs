using System.ComponentModel.DataAnnotations;

namespace SupportTriage.Application.Tickets.Settings;

public sealed class SeedDataSettings : IValidatableObject
{
    public const string SectionName = "SeedData";

    public bool ImportOnStartup { get; set; }

    // Relative paths are resolved against the application's base directory.
    public string? FilePath { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ImportOnStartup && string.IsNullOrWhiteSpace(FilePath))
            yield return new ValidationResult("FilePath is required when ImportOnStartup is true.", [nameof(FilePath)]);
    }
}
