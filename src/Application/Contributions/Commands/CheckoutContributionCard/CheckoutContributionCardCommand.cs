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

namespace Casamento.Application.Contributions.Commands.CheckoutContributionCard;

public sealed record CheckoutContributionCardCommand(
    decimal Amount,
    string ContributorName,
    string ContributorEmail,
    string ContributorDocument,
    string Phone,
    string PostalCode,
    string AddressNumber,
    string CardHolderName,
    string CardNumber,
    string CardExpiryMonth,
    string CardExpiryYear,
    string CardCcv,
    string? Message,
    string IpHash) : IRequest<Result<CheckoutContributionCardResultDto>>;

public sealed class CheckoutContributionCardValidator : AbstractValidator<CheckoutContributionCardCommand>
{
    public CheckoutContributionCardValidator()
    {
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(Contribution.MinAmount);
        RuleFor(x => x.ContributorName).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(x => x.ContributorEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.ContributorDocument).NotEmpty().Must(HasValidDigits).WithMessage("CPF ou CNPJ inválido.");
        RuleFor(x => x.Phone).NotEmpty().Must(x => x.Count(char.IsDigit) is >= 10 and <= 11);
        RuleFor(x => x.PostalCode).NotEmpty().Must(x => x.Count(char.IsDigit) == 8).WithMessage("CEP inválido.");
        RuleFor(x => x.AddressNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x.CardHolderName).NotEmpty().MinimumLength(2);
        RuleFor(x => x.CardNumber).NotEmpty().Must(x => x.Count(char.IsDigit) is >= 13 and <= 19);
        RuleFor(x => x.CardExpiryMonth).NotEmpty().Matches("^(0?[1-9]|1[0-2])$");
        RuleFor(x => x.CardExpiryYear).NotEmpty().Matches(@"^\d{4}$");
        RuleFor(x => x.CardCcv).NotEmpty().Matches(@"^\d{3,4}$");
        RuleFor(x => x.Message).MaximumLength(Contribution.MaxMessageLength);
        RuleFor(x => x.IpHash).NotEmpty();
    }

    private static bool HasValidDigits(string document)
    {
        var digits = document.Count(char.IsDigit);
        return digits is 11 or 14;
    }
}

public sealed class CheckoutContributionCardHandler(
    IContributionRepository repository,
    IPaymentGateway gateway,
    IContributionPaidNotifier notifier,
    IClock clock,
    ILogger<CheckoutContributionCardHandler> logger)
    : IRequestHandler<CheckoutContributionCardCommand, Result<CheckoutContributionCardResultDto>>
{
    public async Task<Result<CheckoutContributionCardResultDto>> Handle(
        CheckoutContributionCardCommand request,
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

        // Persiste em Pending antes de chamar Asaas — assim o ID já existe pra
        // eventual rastreamento via webhook idempotente.
        await repository.AddAsync(contribution, cancellationToken).ConfigureAwait(false);

        PaymentCardResult card;
        try
        {
            card = await gateway.CreateCardPaymentAsync(
                new CreateCardPaymentRequest(
                    GiftId: contribution.Id,
                    AmountBrl: amount.Amount,
                    GuestName: request.ContributorName,
                    GuestEmail: request.ContributorEmail,
                    GuestDocument: request.ContributorDocument,
                    Description: $"Contribuição casamento Bruna & Pedro",
                    DueDate: now.AddDays(1),
                    RemoteIp: request.IpHash,
                    Card: new CreditCardData(
                        HolderName: request.CardHolderName,
                        Number: request.CardNumber,
                        ExpiryMonth: request.CardExpiryMonth,
                        ExpiryYear: request.CardExpiryYear,
                        Ccv: request.CardCcv),
                    HolderInfo: new CreditCardHolderData(
                        PostalCode: request.PostalCode,
                        AddressNumber: request.AddressNumber,
                        Phone: request.Phone)),
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao cobrar cartão para contribution {ContributionId}", contribution.Id);
            return Error.External("Não foi possível processar o cartão. Verifique os dados e tente de novo.");
        }

        if (card.Status == PaymentCardStatus.Confirmed)
        {
            try
            {
                contribution.MarkPaid(request.ContributorName, contributorEmail, card.ProviderPaymentId, now, request.Message);
            }
            catch (DomainException ex)
            {
                logger.LogError(ex, "Erro ao marcar contribution {ContributionId} como paga após cartão Confirmed", contribution.Id);
                return Error.Validation(ex.Message);
            }

            await repository.UpdateAsync(contribution, cancellationToken).ConfigureAwait(false);

            try
            {
                await notifier.NotifyAsync(
                    contribution,
                    new PaymentCustomer(request.ContributorName, request.ContributorEmail),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar e-mails (cartão) para contribution {ContributionId}", contribution.Id);
            }
        }

        return new CheckoutContributionCardResultDto(contribution.Id, card.ProviderPaymentId, card.Status.ToString());
    }
}
