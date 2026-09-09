using Casamento.Application.Abstractions;
using Casamento.Application.Contributions.Commands.ConfirmManualContributionPix;
using Casamento.Application.Contributions.Notifications;
using Casamento.Domain.Contributions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Casamento.Application.Tests.Contributions;

public sealed class ConfirmManualContributionPixHandlerTests
{
    private readonly IContributionRepository _repo = Substitute.For<IContributionRepository>();
    private readonly IContributionPaidNotifier _notifier = Substitute.For<IContributionPaidNotifier>();
    private readonly TestClock _clock = new();

    private ConfirmManualContributionPixHandler CreateHandler() =>
        new(_repo, _notifier, _clock, NullLogger<ConfirmManualContributionPixHandler>.Instance);

    [Fact]
    public async Task Creates_contribution_marked_as_paid_and_notifies()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ConfirmManualContributionPixCommand(
                Amount: 50m,
                ContributorName: "Fulano",
                ContributorEmail: "f@x.com",
                Message: "Parabéns!",
                IpHash: "iphash"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Confirmed");
        await _repo.Received(1).AddAsync(
            Arg.Is<Contribution>(c => c.Status == ContributionStatus.Paid && c.Amount.Amount == 50m),
            Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyAsync(
            Arg.Any<Contribution>(),
            Arg.Any<PaymentCustomer>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_amount_below_minimum()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ConfirmManualContributionPixCommand(
                Amount: 3m,
                ContributorName: "Fulano",
                ContributorEmail: "f@x.com",
                Message: null,
                IpHash: "iphash"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddAsync(Arg.Any<Contribution>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_invalid_email()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ConfirmManualContributionPixCommand(
                Amount: 50m,
                ContributorName: "Fulano",
                ContributorEmail: "notanemail",
                Message: null,
                IpHash: "iphash"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddAsync(Arg.Any<Contribution>(), Arg.Any<CancellationToken>());
    }
}
