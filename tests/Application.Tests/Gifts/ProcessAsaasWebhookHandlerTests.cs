using Casamento.Application.Abstractions;
using Casamento.Application.Gifts.Commands.ProcessAsaasWebhook;
using Casamento.Application.Gifts.Notifications;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Casamento.Application.Tests.Gifts;

public sealed class ProcessAsaasWebhookHandlerTests
{
    private readonly IGiftRepository _giftRepo = Substitute.For<IGiftRepository>();
    private readonly IPaymentEventRepository _eventRepo = Substitute.For<IPaymentEventRepository>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly IGiftPaidNotifier _notifier = Substitute.For<IGiftPaidNotifier>();
    private readonly TestClock _clock = new();

    private ProcessAsaasWebhookHandler CreateHandler() =>
        new(_giftRepo, _eventRepo, _gateway, _notifier, _clock, NullLogger<ProcessAsaasWebhookHandler>.Instance);

    [Fact]
    public async Task Confirmed_payment_marks_available_gift_as_paid_and_notifies()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, _clock.UtcNow);

        _eventRepo.TryRecordAsync(Arg.Any<PaymentEvent>(), Arg.Any<CancellationToken>()).Returns(true);
        _giftRepo.GetByIdAsync(gift.Id, Arg.Any<CancellationToken>()).Returns(gift);
        _gateway.GetCustomerAsync("cus_1", Arg.Any<CancellationToken>())
            .Returns(new PaymentCustomer("Fulano", "f@x.com"));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ProcessAsaasWebhookCommand("evt_1", "PAYMENT_CONFIRMED", "pay_1", "cus_1", gift.Id.ToString("N"), "{}"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        gift.Status.Should().Be(GiftStatus.Paid);
        gift.Purchase!.BuyerEmail.Value.Should().Be("f@x.com");
        await _giftRepo.Received(1).UpdateAsync(gift, Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyAsync(gift, Arg.Any<PaymentCustomer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_event_is_ignored()
    {
        _eventRepo.TryRecordAsync(Arg.Any<PaymentEvent>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ProcessAsaasWebhookCommand("evt_1", "PAYMENT_CONFIRMED", "pay_1", "cus_1", null, "{}"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _giftRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _notifier.DidNotReceive().NotifyAsync(Arg.Any<Gift>(), Arg.Any<PaymentCustomer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_payment_events_are_ignored()
    {
        _eventRepo.TryRecordAsync(Arg.Any<PaymentEvent>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ProcessAsaasWebhookCommand("evt_2", "PAYMENT_REFUNDED", "pay_1", "cus_1", null, "{}"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _giftRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _notifier.DidNotReceive().NotifyAsync(Arg.Any<Gift>(), Arg.Any<PaymentCustomer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_gift_does_not_fail()
    {
        _eventRepo.TryRecordAsync(Arg.Any<PaymentEvent>(), Arg.Any<CancellationToken>()).Returns(true);
        _giftRepo.FindByAsaasPaymentIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Gift?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ProcessAsaasWebhookCommand("evt_3", "PAYMENT_CONFIRMED", "pay_unknown", "cus_1", null, "{}"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Second_payment_for_paid_gift_is_logged_and_not_notified()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, _clock.UtcNow);
        gift.MarkPaid("First", Casamento.Domain.Rsvps.ValueObjects.Email.Create("first@x.com"), "pay_1", _clock.UtcNow);

        _eventRepo.TryRecordAsync(Arg.Any<PaymentEvent>(), Arg.Any<CancellationToken>()).Returns(true);
        _giftRepo.GetByIdAsync(gift.Id, Arg.Any<CancellationToken>()).Returns(gift);
        _gateway.GetCustomerAsync("cus_2", Arg.Any<CancellationToken>())
            .Returns(new PaymentCustomer("Second", "second@x.com"));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ProcessAsaasWebhookCommand("evt_4", "PAYMENT_CONFIRMED", "pay_2", "cus_2", gift.Id.ToString("N"), "{}"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        gift.Purchase!.AsaasPaymentId.Should().Be("pay_1");
        await _notifier.DidNotReceive().NotifyAsync(Arg.Any<Gift>(), Arg.Any<PaymentCustomer>(), Arg.Any<CancellationToken>());
    }
}
