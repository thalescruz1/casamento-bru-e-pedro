using Casamento.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Casamento.Infrastructure.Messaging;

internal sealed class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "E-mail NÃO enviado (ACS não configurado) — destinatário {Email}, assunto \"{Subject}\"",
            message.ToEmail, message.Subject);
        return Task.CompletedTask;
    }
}
