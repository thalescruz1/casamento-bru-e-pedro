using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Asaas;

public sealed class AsaasOptions
{
    public const string SectionName = "Asaas";

    [Required]
    public Uri BaseUrl { get; set; } = new("https://sandbox.asaas.com/api/v3/");

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Token usado no webhook de validação de saque (separado do webhook de pagamento).
    /// Configurado em Asaas → Mecanismos de segurança → Validação de saque via Webhook.
    /// </summary>
    public string WithdrawalWebhookToken { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// URL do proxy HTTP pra rotear chamadas Asaas (e ter IP fixo na whitelist).
    /// Ex: "http://20.110.219.191:3128". Se vazio, chama Asaas direto.
    /// </summary>
    public string ProxyUrl { get; set; } = string.Empty;

    public string ProxyUsername { get; set; } = string.Empty;

    public string ProxyPassword { get; set; } = string.Empty;
}

internal sealed class AsaasOptionsValidator : IValidateOptions<AsaasOptions>
{
    public ValidateOptionsResult Validate(string? name, AsaasOptions options)
    {
        if (options.BaseUrl is null || !options.BaseUrl.IsAbsoluteUri)
        {
            return ValidateOptionsResult.Fail("Asaas:BaseUrl inválido.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail("Asaas:ApiKey obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            return ValidateOptionsResult.Fail("Asaas:WebhookSecret obrigatório.");
        }

        return ValidateOptionsResult.Success;
    }
}
