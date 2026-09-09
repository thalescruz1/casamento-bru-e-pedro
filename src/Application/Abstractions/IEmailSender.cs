namespace Casamento.Application.Abstractions;

public sealed record EmailMessage(
    string ToName,
    string ToEmail,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
