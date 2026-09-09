using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Messaging;

public sealed class AcsEmailOptions
{
    public const string SectionName = "AcsEmail";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string SenderAddress { get; set; } = string.Empty;

    public string SenderDisplayName { get; set; } = "Helô & Thales";
}

internal sealed class AcsEmailOptionsValidator : IValidateOptions<AcsEmailOptions>
{
    public ValidateOptionsResult Validate(string? name, AcsEmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("AcsEmail:ConnectionString é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(options.SenderAddress))
        {
            return ValidateOptionsResult.Fail("AcsEmail:SenderAddress é obrigatório.");
        }

        return ValidateOptionsResult.Success;
    }
}
