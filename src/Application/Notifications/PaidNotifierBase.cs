using System.Globalization;
using Casamento.Application.Abstractions;
using Casamento.Application.Gifts.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casamento.Application.Notifications;

/// <summary>
/// Base para notificadores de pagamento (Gift, Contribution). Encapsula
/// <see cref="WeddingEmailOptions"/>, formatação BRL e helpers para envio de e-mail
/// (comprador + noivos com tratamento de exceção via logger).
/// </summary>
public abstract class PaidNotifierBase
{
    // Formato pt-BR construído manualmente — InvariantGlobalization=true bloqueia CultureInfo.GetCultureInfo("pt-BR").
    private static readonly NumberFormatInfo BrlFormat = new()
    {
        CurrencySymbol = "R$",
        CurrencyDecimalSeparator = ",",
        CurrencyGroupSeparator = ".",
        CurrencyGroupSizes = new[] { 3 },
        CurrencyDecimalDigits = 2,
        CurrencyPositivePattern = 2 // "R$ 1.234,56"
    };

    private readonly IEmailSender _emailSender;
    private readonly ILogger _logger;

    protected PaidNotifierBase(
        IEmailSender emailSender,
        IOptions<WeddingEmailOptions> options,
        ILogger logger)
    {
        _emailSender = emailSender;
        Options = options.Value;
        _logger = logger;
    }

    protected WeddingEmailOptions Options { get; }

    protected static string FormatBrl(decimal amount) => amount.ToString("C", BrlFormat);

    protected async Task SendBuyerEmailAsync(
        PaymentCustomer customer,
        string subject,
        string htmlBody,
        string plainTextBody,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailSender.SendAsync(new EmailMessage(
                ToName: customer.Name,
                ToEmail: customer.Email,
                Subject: subject,
                HtmlBody: htmlBody,
                PlainTextBody: plainTextBody),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar recibo para {BuyerEmail}", customer.Email);
        }
    }

    protected async Task SendCoupleEmailsAsync(
        string subject,
        string htmlBody,
        string plainTextBody,
        CancellationToken cancellationToken)
    {
        foreach (var recipient in Options.Couple)
        {
            if (string.IsNullOrWhiteSpace(recipient.Email))
            {
                continue;
            }

            try
            {
                await _emailSender.SendAsync(new EmailMessage(
                    ToName: string.IsNullOrWhiteSpace(recipient.Name) ? recipient.Email : recipient.Name,
                    ToEmail: recipient.Email,
                    Subject: subject,
                    HtmlBody: htmlBody,
                    PlainTextBody: plainTextBody),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar notificação para {Recipient}", recipient.Email);
            }
        }
    }
}
