using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Contributions.Dtos;
using Casamento.Domain.Contributions;
using FluentValidation;
using MediatR;

namespace Casamento.Application.Contributions.Commands.CheckoutContributionPix;

public sealed record CheckoutContributionPixCommand(decimal Amount)
    : IRequest<Result<CheckoutContributionPixResultDto>>;

public sealed class CheckoutContributionPixValidator : AbstractValidator<CheckoutContributionPixCommand>
{
    public CheckoutContributionPixValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(Contribution.MinAmount)
            .WithMessage($"Valor mínimo de contribuição é R$ {Contribution.MinAmount:0.00}.")
            .LessThanOrEqualTo(1_000_000m);
    }
}

public sealed class CheckoutContributionPixHandler(
    IOwnPixProvider pixProvider,
    IPixQrCodeBuilder qrBuilder)
    : IRequestHandler<CheckoutContributionPixCommand, Result<CheckoutContributionPixResultDto>>
{
    public Task<Result<CheckoutContributionPixResultDto>> Handle(
        CheckoutContributionPixCommand request,
        CancellationToken cancellationToken)
    {
        var pix = pixProvider.Get();
        var amount = decimal.Round(request.Amount, 2, MidpointRounding.ToEven);
        var reference = $"HTC{Guid.NewGuid().ToString("N")[..9]}";
        var qr = qrBuilder.Build(pix, amount, reference);

        var dto = new CheckoutContributionPixResultDto(
            pix.Key,
            pix.KeyType,
            pix.Beneficiary,
            pix.Bank,
            amount,
            qr.Payload,
            qr.ImageBase64);

        return Task.FromResult(Result<CheckoutContributionPixResultDto>.Success(dto));
    }
}
