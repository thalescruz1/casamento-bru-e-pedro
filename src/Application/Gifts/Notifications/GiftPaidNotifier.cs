using Casamento.Application.Abstractions;
using Casamento.Application.Notifications;
using Casamento.Domain.Gifts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casamento.Application.Gifts.Notifications;

public interface IGiftPaidNotifier
{
    Task NotifyAsync(Gift gift, PaymentCustomer customer, CancellationToken cancellationToken);
}

public sealed class WeddingEmailOptions
{
    public const string SectionName = "WeddingEmail";

    public string CoupleName { get; set; } = "Helô & Thales";

    public IReadOnlyList<CoupleRecipient> Couple { get; set; } = Array.Empty<CoupleRecipient>();

    public sealed class CoupleRecipient
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

internal sealed class GiftPaidNotifier(
    IEmailSender emailSender,
    IOptions<WeddingEmailOptions> options,
    ILogger<GiftPaidNotifier> logger)
    : PaidNotifierBase(emailSender, options, logger), IGiftPaidNotifier
{
    public async Task NotifyAsync(Gift gift, PaymentCustomer customer, CancellationToken cancellationToken)
    {
        var priceFormatted = FormatBrl(gift.Price.Amount);

        await SendBuyerEmailAsync(
            customer,
            subject: $"Obrigada pelo presente — {Options.CoupleName}",
            htmlBody: BuildBuyerHtml(gift, customer, priceFormatted),
            plainTextBody: BuildBuyerPlainText(gift, customer, priceFormatted),
            cancellationToken).ConfigureAwait(false);

        await SendCoupleEmailsAsync(
            subject: $"Novo presente recebido — {gift.Title}",
            htmlBody: BuildCoupleHtml(gift, customer, priceFormatted),
            plainTextBody: BuildCouplePlainText(gift, customer, priceFormatted),
            cancellationToken).ConfigureAwait(false);
    }

    private string BuildBuyerHtml(Gift gift, PaymentCustomer customer, string price) => $$"""
<!doctype html>
<html lang="pt-BR"><head><meta charset="utf-8"/></head>
<body style="font-family: Georgia, 'Cormorant Garamond', serif; color:#14110d; background:#f2ede4; margin:0; padding:40px 20px;">
  <div style="max-width:560px; margin:0 auto; background:#ece6db; padding:40px; border-radius:4px;">
    <p style="letter-spacing:.3em; text-transform:uppercase; font-size:12px; margin:0 0 16px;">{{Options.CoupleName}}</p>
    <h1 style="font-weight:300; font-size:28px; margin:0 0 24px;">Obrigada, {{customer.Name}}!</h1>
    <p>Sua contribuição <em>{{gift.Title}}</em> ({{price}}) chegou com todo carinho. ❤️</p>
    <p>Agradecemos de coração por fazer parte desse momento tão especial.</p>
    <p style="margin-top:32px;">Com amor,<br/>{{Options.CoupleName}}</p>
  </div>
</body></html>
""";

    private string BuildBuyerPlainText(Gift gift, PaymentCustomer customer, string price) =>
        $"Obrigada, {customer.Name}!\n\nSua contribuição \"{gift.Title}\" ({price}) chegou com todo carinho.\n\nCom amor,\n{Options.CoupleName}";

    private static string BuildCoupleHtml(Gift gift, PaymentCustomer customer, string price)
    {
        var messageBlock = string.IsNullOrWhiteSpace(gift.Purchase?.Message)
            ? string.Empty
            : $"<p style=\"margin-top:16px; padding:14px; background:#f2ede4; border-left:3px solid #14110d;\"><strong>Mensagem:</strong><br/>{System.Net.WebUtility.HtmlEncode(gift.Purchase!.Message)}</p>";

        return $$"""
<!doctype html>
<html lang="pt-BR"><head><meta charset="utf-8"/></head>
<body style="font-family: Georgia, serif; color:#14110d; background:#f2ede4; margin:0; padding:40px 20px;">
  <div style="max-width:560px; margin:0 auto; background:#ece6db; padding:40px; border-radius:4px;">
    <p style="letter-spacing:.3em; text-transform:uppercase; font-size:12px; margin:0 0 16px;">Novo presente</p>
    <h1 style="font-weight:300; font-size:24px; margin:0 0 24px;">{{gift.Title}}</h1>
    <p><strong>Valor:</strong> {{price}}</p>
    <p><strong>Comprador:</strong> {{customer.Name}} &lt;{{customer.Email}}&gt;</p>
    <p><strong>ID:</strong> {{gift.Purchase?.AsaasPaymentId}}</p>
    {{messageBlock}}
    <p style="margin-top:32px; font-size:13px; color:#6b6458;">Enviado automaticamente pelo site.</p>
  </div>
</body></html>
""";
    }

    private static string BuildCouplePlainText(Gift gift, PaymentCustomer customer, string price)
    {
        var msg = string.IsNullOrWhiteSpace(gift.Purchase?.Message)
            ? string.Empty
            : $"\nMensagem: {gift.Purchase!.Message}\n";
        return $"Novo presente recebido!\n\n" +
               $"Item: {gift.Title}\n" +
               $"Valor: {price}\n" +
               $"Comprador: {customer.Name} <{customer.Email}>\n" +
               $"ID: {gift.Purchase?.AsaasPaymentId}\n" +
               msg;
    }
}
