using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Cosmos;

public sealed class CosmosOptions
{
    public const string SectionName = "Cosmos";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = "casamento";

    public string RsvpsContainer { get; set; } = "rsvps";
    public string GiftsContainer { get; set; } = "gifts";
    public string PaymentEventsContainer { get; set; } = "payments";
    public string AdminsContainer { get; set; } = "admins";
    public string ContributionsContainer { get; set; } = "contributions";

    public bool AutoCreate { get; set; } = true;
}

internal sealed class CosmosOptionsValidator : IValidateOptions<CosmosOptions>
{
    public ValidateOptionsResult Validate(string? name, CosmosOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Cosmos:ConnectionString é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseName))
        {
            return ValidateOptionsResult.Fail("Cosmos:DatabaseName é obrigatório.");
        }

        return ValidateOptionsResult.Success;
    }
}
