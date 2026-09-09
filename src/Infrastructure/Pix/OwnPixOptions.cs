using System.ComponentModel.DataAnnotations;

namespace Casamento.Infrastructure.Pix;

public sealed class OwnPixOptions
{
    public const string SectionName = "OwnPix";

    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>"email", "cpf", "phone" ou "random".</summary>
    [Required]
    public string KeyType { get; set; } = string.Empty;

    [Required]
    public string Beneficiary { get; set; } = string.Empty;

    public string? Bank { get; set; }

    /// <summary>Cidade usada no payload EMV Pix (máx 15 caracteres).</summary>
    public string City { get; set; } = "SAO PAULO";
}
