using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Storage;

public sealed class BlobOptions
{
    public const string SectionName = "Blob";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    public string GiftImagesContainer { get; set; } = "gift-images";

    public string? PublicBaseUrl { get; set; }
}

internal sealed class BlobOptionsValidator : IValidateOptions<BlobOptions>
{
    public ValidateOptionsResult Validate(string? name, BlobOptions options) =>
        string.IsNullOrWhiteSpace(options.ConnectionString)
            ? ValidateOptionsResult.Fail("Blob:ConnectionString é obrigatório.")
            : ValidateOptionsResult.Success;
}
