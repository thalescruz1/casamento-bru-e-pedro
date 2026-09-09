using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Chave de assinatura HMAC-SHA256 do token de sessão (mínimo 32 bytes em base64 ou texto).</summary>
    [Required]
    public string TokenSigningKey { get; set; } = string.Empty;

    public int SessionLifetimeHours { get; set; } = 12;

    public string CookieName { get; set; } = "adm_session";

    /// <summary>E-mail do primeiro admin — criado no startup se não existir.</summary>
    public string? SeedEmail { get; set; }

    /// <summary>Senha inicial do primeiro admin (trocar depois do primeiro login).</summary>
    public string? SeedPassword { get; set; }

    public string? SeedDisplayName { get; set; }
}

internal sealed class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TokenSigningKey))
        {
            return ValidateOptionsResult.Fail("Auth:TokenSigningKey é obrigatório.");
        }

        if (options.TokenSigningKey.Length < 32)
        {
            return ValidateOptionsResult.Fail("Auth:TokenSigningKey deve ter pelo menos 32 caracteres.");
        }

        if (options.SessionLifetimeHours is < 1 or > 720)
        {
            return ValidateOptionsResult.Fail("Auth:SessionLifetimeHours fora do intervalo 1..720.");
        }

        return ValidateOptionsResult.Success;
    }
}
