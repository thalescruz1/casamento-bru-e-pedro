using Casamento.Application.Abstractions;
using Casamento.Application.Contributions.Commands.CheckoutContributionCard;
using Casamento.Application.Contributions.Notifications;
using Casamento.Domain.Contributions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Casamento.Application.Tests.Contributions;

public sealed class CheckoutContributionCardHandlerTests
{
    private readonly IContributionRepository _repo = Substitute.For<IContributionRepository>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly IContributionPaidNotifier _notifier = Substitute.For<IContributionPaidNotifier>();
    private readonly TestClock _clock = new();

    private CheckoutContributionCardHandler CreateHandler() =>
        new(_repo, _gateway, _notifier, _clock, NullLogger<CheckoutContributionCardHandler>.Instance);

    private static CheckoutContributionCardCommand BuildCommand(decimal amount = 100m) =>
        new(
            Amount: amount,
            ContributorName: "Fulano da Silva",
            ContributorEmail: "f@x.com",
            ContributorDocument: "39053344705",
            Phone: "11999998888",
            PostalCode: "01310100",
            AddressNumber: "100",
            CardHolderName: "FULANO SILVA",
            CardNumber: "5162306288625968",
            CardExpiryMonth: "12",
            CardExpiryYear: "2030",
            CardCcv: "318",
            Message: "Felicidades",
            IpHash: "iphash");

    [Fact]
    public async Task Confirmed_card_persists_paid_contribution_and_notifies()
    {
        _gateway.CreateCardPaymentAsync(Arg.Any<CreateCardPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentCardResult("pay_1", PaymentCardStatus.Confirmed, "https://invoice"));

        var handler = CreateHandler();
        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Confirmed");
        result.Value.AsaasPaymentId.Should().Be("pay_1");
        await _repo.Received(1).AddAsync(Arg.Any<Contribution>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).UpdateAsync(
            Arg.Is<Contribution>(c => c.Status == ContributionStatus.Paid),
            Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyAsync(
            Arg.Any<Contribution>(),
            Arg.Any<PaymentCustomer>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pending_card_keeps_contribution_pending_no_notification()
    {
        _gateway.CreateCardPaymentAsync(Arg.Any<CreateCardPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentCardResult("pay_2", PaymentCardStatus.Pending, null));

        var handler = CreateHandler();
        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Pending");
        await _repo.Received(1).AddAsync(Arg.Any<Contribution>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Contribution>(), Arg.Any<CancellationToken>());
        await _notifier.DidNotReceive().NotifyAsync(
            Arg.Any<Contribution>(),
            Arg.Any<PaymentCustomer>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Gateway_failure_returns_external_error()
    {
        _gateway.CreateCardPaymentAsync(Arg.Any<CreateCardPaymentRequest>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("boom"));

        var handler = CreateHandler();
        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("external");
    }

    [Fact]
    public async Task Amount_below_minimum_is_rejected_before_calling_gateway()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(BuildCommand(amount: 1m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _gateway.DidNotReceive().CreateCardPaymentAsync(Arg.Any<CreateCardPaymentRequest>(), Arg.Any<CancellationToken>());
    }
}
