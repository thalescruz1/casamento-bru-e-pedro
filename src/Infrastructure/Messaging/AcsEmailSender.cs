using Azure;
using Azure.Communication.Email;
using Casamento.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AcsEmailMessage = Azure.Communication.Email.EmailMessage;
using AppEmailMessage = Casamento.Application.Abstractions.EmailMessage;

namespace Casamento.Infrastructure.Messaging;

internal sealed class AcsEmailSender(
    EmailClient client,
    IOptions<AcsEmailOptions> options,
    ILogger<AcsEmailSender> logger) : IEmailSender
{
    private readonly AcsEmailOptions _options = options.Value;

    public async Task SendAsync(AppEmailMessage message, CancellationToken cancellationToken)
    {
        var acsMessage = new AcsEmailMessage(
            senderAddress: _options.SenderAddress,
            recipientAddress: message.ToEmail,
            content: new EmailContent(message.Subject)
            {
                Html = message.HtmlBody,
                PlainText = message.PlainTextBody ?? StripHtml(message.HtmlBody)
            });

        try
        {
            var operation = await client.SendAsync(
                WaitUntil.Started,
                acsMessage,
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation("ACS e-mail enviado para {Email} (op {OperationId})", message.ToEmail, operation.Id);
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "ACS recusou envio para {Email}: {Status} {Code}", message.ToEmail, ex.Status, ex.ErrorCode);
            throw;
        }
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
}
