using Casamento.Application.Abstractions;
using Casamento.Application.Gifts.Commands.ConfirmManualPix;
using Casamento.Application.Gifts.Commands.ProcessAsaasWebhook;
using Casamento.Application.Gifts.Notifications;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Payments;
using Casamento.Domain.Rsvps.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Casamento.Application.Tests.Gifts;

public sealed class MultiplePurchaseHandlerTests
{
    private readonly IGiftRepository _giftRepo = Substitute.For<IGiftRepository>();
    private readonly IGiftPaidNotifier _notifier = Substitute.For<IGiftPaidNotifier>();
    private readonly TestClock _clock = new();

    [Fact]
    public async Task Manual_pix_on_a_gift_with_remaining_purchases_keeps_it_available()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, _clock.UtcNow, maxPurchases: 3);
        _giftRepo.GetByIdAsync(gift.Id, Arg.Any<CancellationToken>()).Returns(gift);

        var handler = new ConfirmManualPixHandler(_giftRepo, _notifier, _clock, NullLogger<ConfirmManualPixHandler>.Instance);
        var result = await handler.Handle(
            new ConfirmManualPixCommand(gift.Id, "Fulano", "f@x.com", null, "hash"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        gift.PurchaseCount.Should().Be(1);
        gift.Status.Should().Be(GiftStatus.Available);
        await _notifier.Received(1).NotifyAsync(gift, Arg.Any<PaymentCustomer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Manual_pix_is_rejected_once_the_gift_is_sold_out()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, _clock.UtcNow, maxPurchases: 1);
        gift.MarkPaid("Primeiro", Email.Create("p@x.com"), "pay_1", _clock.UtcNow);
        _giftRepo.GetByIdAsync(gift.Id, Arg.Any<CancellationToken>()).Returns(gift);

        var handler = new ConfirmManualPixHandler(_giftRepo, _notifier, _clock, NullLogger<ConfirmManualPixHandler>.Instance);
        var result = await handler.Handle(
            new ConfirmManualPixCommand(gift.Id, "Fulano", "f@x.com", null, "hash"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        gift.PurchaseCount.Should().Be(1);
    }

    [Fact]
    public async Task Concurrent_write_is_retried_so_no_purchase_is_lost()
    {
        var stale = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, _clock.UtcNow, maxPurchases: 5);
        var fresh = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, _clock.UtcNow, maxPurchases: 5);
        fresh.MarkPaid("Outro", Email.Create("o@x.com"), "pay_other", _clock.UtcNow);

        // A primeira leitura é a versão antiga; a gravação dela perde a corrida; a releitura já traz a outra compra.
        _giftRepo.GetByIdAsync(stale.Id, Arg.Any<CancellationToken>()).Returns(stale, fresh);
        _giftRepo.UpdateAsync(stale, Arg.Any<CancellationToken>()).ThrowsAsync(new GiftConcurrencyException(stale.Id));

        var handler = new ConfirmManualPixHandler(_giftRepo, _notifier, _clock, NullLogger<ConfirmManualPixHandler>.Instance);
        var result = await handler.Handle(
            new ConfirmManualPixCommand(stale.Id, "Fulano", "f@x.com", null, "hash"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        fresh.PurchaseCount.Should().Be(2);
        await _giftRepo.Received(1).UpdateAsync(fresh, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Webhook_records_each_payment_and_ignores_a_repeated_one()
    {
        var eventRepo = Substitute.For<IPaymentEventRepository>();
        var gateway = Substitute.For<IPaymentGateway>();
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, _clock.UtcNow, maxPurchases: null);

        eventRepo.TryRecordAsync(Arg.Any<PaymentEvent>(), Arg.Any<CancellationToken>()).Returns(true);
        _giftRepo.GetByIdAsync(gift.Id, Arg.Any<CancellationToken>()).Returns(gift);
        gateway.GetCustomerAsync("cus_1", Arg.Any<CancellationToken>()).Returns(new PaymentCustomer("Fulano", "f@x.com"));
        gateway.GetCustomerAsync("cus_2", Arg.Any<CancellationToken>()).Returns(new PaymentCustomer("Beltrano", "b@x.com"));

        var handler = new ProcessAsaasWebhookHandler(
            _giftRepo, eventRepo, gateway, _notifier, _clock, NullLogger<ProcessAsaasWebhookHandler>.Instance);

        await handler.Handle(
            new ProcessAsaasWebhookCommand("evt_1", "PAYMENT_CONFIRMED", "pay_1", "cus_1", gift.Id.ToString("N"), "{}"),
            CancellationToken.None);
        await handler.Handle(
            new ProcessAsaasWebhookCommand("evt_2", "PAYMENT_CONFIRMED", "pay_2", "cus_2", gift.Id.ToString("N"), "{}"),
            CancellationToken.None);
        await handler.Handle(
            new ProcessAsaasWebhookCommand("evt_3", "PAYMENT_RECEIVED", "pay_2", "cus_2", gift.Id.ToString("N"), "{}"),
            CancellationToken.None);

        gift.PurchaseCount.Should().Be(2);
        gift.Status.Should().Be(GiftStatus.Available);
        await _notifier.Received(2).NotifyAsync(gift, Arg.Any<PaymentCustomer>(), Arg.Any<CancellationToken>());
    }
}
