using Casamento.Application.Abstractions;
using Casamento.Application.Gifts.Notifications;
using Casamento.Application.Notifications;
using Casamento.Domain.Contributions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casamento.Application.Contributions.Notifications;

public interface IContributionPaidNotifier
{
    Task NotifyAsync(Contribution contribution, PaymentCustomer customer, CancellationToken cancellationToken);
}

internal sealed class ContributionPaidNotifier(
    IEmailSender emailSender,
    IOptions<WeddingEmailOptions> options,
    ILogger<ContributionPaidNotifier> logger)
    : PaidNotifierBase(emailSender, options, logger), IContributionPaidNotifier
{
    public async Task NotifyAsync(Contribution contribution, PaymentCustomer customer, CancellationToken cancellationToken)
    {
        var amountFormatted = FormatBrl(contribution.Amount.Amount);

        await SendBuyerEmailAsync(
            customer,
            subject: $"Obrigada pela contribuição — {Options.CoupleName}",
            htmlBody: BuildBuyerHtml(customer, amountFormatted),
            plainTextBody: BuildBuyerPlainText(customer, amountFormatted),
            cancellationToken).ConfigureAwait(false);

        await SendCoupleEmailsAsync(
            subject: $"Nova contribuição recebida — {amountFormatted}",
            htmlBody: BuildCoupleHtml(contribution, customer, amountFormatted),
            plainTextBody: BuildCouplePlainText(contribution, customer, amountFormatted),
            cancellationToken).ConfigureAwait(false);
    }

    private string BuildBuyerHtml(PaymentCustomer customer, string amount) => $$"""
<!doctype html>
<html lang="pt-BR"><head><meta charset="utf-8"/></head>
<body style="font-family: Georgia, 'Cormorant Garamond', serif; color:#14110d; background:#f2ede4; margin:0; padding:40px 20px;">
  <div style="max-width:560px; margin:0 auto; background:#ece6db; padding:40px; border-radius:4px;">
    <p style="letter-spacing:.3em; text-transform:uppercase; font-size:12px; margin:0 0 16px;">{{Options.CoupleName}}</p>
    <h1 style="font-weight:300; font-size:28px; margin:0 0 24px;">Obrigada, {{customer.Name}}!</h1>
    <p>Sua contribuição de <strong>{{amount}}</strong> chegou com todo carinho. ❤️</p>
    <p>Agradecemos de coração por fazer parte desse momento tão especial.</p>
    <p style="margin-top:32px;">Com amor,<br/>{{Options.CoupleName}}</p>
  </div>
</body></html>
""";

    private string BuildBuyerPlainText(PaymentCustomer customer, string amount) =>
        $"Obrigada, {customer.Name}!\n\nSua contribuição de {amount} chegou com todo carinho.\n\nCom amor,\n{Options.CoupleName}";

    private static string BuildCoupleHtml(Contribution contribution, PaymentCustomer customer, string amount)
    {
        var messageBlock = string.IsNullOrWhiteSpace(contribution.Message)
            ? string.Empty
            : $"<p style=\"margin-top:16px; padding:14px; background:#f2ede4; border-left:3px solid #14110d;\"><strong>Mensagem:</strong><br/>{System.Net.WebUtility.HtmlEncode(contribution.Message)}</p>";

        return $$"""
<!doctype html>
<html lang="pt-BR"><head><meta charset="utf-8"/></head>
<body style="font-family: Georgia, serif; color:#14110d; background:#f2ede4; margin:0; padding:40px 20px;">
  <div style="max-width:560px; margin:0 auto; background:#ece6db; padding:40px; border-radius:4px;">
    <p style="letter-spacing:.3em; text-transform:uppercase; font-size:12px; margin:0 0 16px;">Nova contribuição</p>
    <h1 style="font-weight:300; font-size:24px; margin:0 0 24px;">{{amount}}</h1>
    <p><strong>Contribuinte:</strong> {{customer.Name}} &lt;{{customer.Email}}&gt;</p>
    <p><strong>ID:</strong> {{contribution.AsaasPaymentId}}</p>
    {{messageBlock}}
    <p style="margin-top:32px; font-size:13px; color:#6b6458;">Enviado automaticamente pelo site.</p>
  </div>
</body></html>
""";
    }

    private static string BuildCouplePlainText(Contribution contribution, PaymentCustomer customer, string amount)
    {
        var msg = string.IsNullOrWhiteSpace(contribution.Message)
            ? string.Empty
            : $"\nMensagem: {contribution.Message}\n";
        return $"Nova contribuição recebida!\n\n" +
               $"Valor: {amount}\n" +
               $"Contribuinte: {customer.Name} <{customer.Email}>\n" +
               $"ID: {contribution.AsaasPaymentId}\n" +
               msg;
    }
}
