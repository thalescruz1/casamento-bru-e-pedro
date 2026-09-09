using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Contributions.Dtos;
using Casamento.Application.Contributions.Notifications;
using Casamento.Domain.Common;
using Casamento.Domain.Contributions;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Casamento.Application.Contributions.Commands.ConfirmManualContributionPix;

public sealed record ConfirmManualContributionPixCommand(
    decimal Amount,
    string ContributorName,
    string ContributorEmail,
    string? Message,
    string IpHash) : IRequest<Result<ConfirmManualContributionPixResultDto>>;

public sealed class ConfirmManualContributionPixValidator : AbstractValidator<ConfirmManualContributionPixCommand>
{
    public ConfirmManualContributionPixValidator()
    {
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(Contribution.MinAmount);
        RuleFor(x => x.ContributorName).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(x => x.ContributorEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Message).MaximumLength(Contribution.MaxMessageLength);
        RuleFor(x => x.IpHash).NotEmpty();
    }
}

public sealed class ConfirmManualContributionPixHandler(
    IContributionRepository repository,
    IContributionPaidNotifier notifier,
    IClock clock,
    ILogger<ConfirmManualContributionPixHandler> logger)
    : IRequestHandler<ConfirmManualContributionPixCommand, Result<ConfirmManualContributionPixResultDto>>
{
    public async Task<Result<ConfirmManualContributionPixResultDto>> Handle(
        ConfirmManualContributionPixCommand request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        Money amount;
        Email contributorEmail;
        try
        {
            amount = Money.FromBrl(request.Amount);
            contributorEmail = Email.Create(request.ContributorEmail);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        Contribution contribution;
        try
        {
            contribution = Contribution.Create(amount, now);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        var manualPaymentId = $"manual-contrib-{Guid.NewGuid():N}";

        try
        {
            contribution.MarkPaid(request.ContributorName, contributorEmail, manualPaymentId, now, request.Message);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        await repository.AddAsync(contribution, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Contribution {ContributionId} ({Amount}) marcada como paga via Pix manual por {Contributor}.",
            contribution.Id, amount.Amount, request.ContributorEmail);

        try
        {
            await notifier.NotifyAsync(
                contribution,
                new PaymentCustomer(request.ContributorName, request.ContributorEmail),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar e-mails de confirmação (Pix manual) para contribution {ContributionId}", contribution.Id);
        }

        return new ConfirmManualContributionPixResultDto(contribution.Id, "Confirmed");
    }
}
