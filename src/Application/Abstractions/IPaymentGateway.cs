namespace Casamento.Application.Abstractions;

public sealed record CreatePixPaymentRequest(
    Guid GiftId,
    decimal AmountBrl,
    string GuestName,
    string GuestEmail,
    string GuestDocument,
    string Description,
    DateTimeOffset DueDate);

public sealed record CreateCardPaymentRequest(
    Guid GiftId,
    decimal AmountBrl,
    string GuestName,
    string GuestEmail,
    string GuestDocument,
    string Description,
    DateTimeOffset DueDate,
    string RemoteIp,
    CreditCardData Card,
    CreditCardHolderData HolderInfo);

public sealed record CreditCardData(
    string HolderName,
    string Number,
    string ExpiryMonth,
    string ExpiryYear,
    string Ccv);

public sealed record CreditCardHolderData(
    string PostalCode,
    string AddressNumber,
    string Phone);

public sealed record PaymentPixResult(
    string ProviderPaymentId,
    string QrCodeImageBase64,
    string QrCodePayload,
    DateTimeOffset ExpirationDate);

public sealed record PaymentCardResult(
    string ProviderPaymentId,
    PaymentCardStatus Status,
    string? InvoiceUrl);

public enum PaymentCardStatus
{
    Confirmed,
    Pending,
    Refused
}

public sealed record PaymentCustomer(string Name, string Email);

public interface IPaymentGateway
{
    Task<PaymentPixResult> CreatePixPaymentAsync(CreatePixPaymentRequest request, CancellationToken cancellationToken);

    Task<PaymentCardResult> CreateCardPaymentAsync(CreateCardPaymentRequest request, CancellationToken cancellationToken);

    Task<PaymentCustomer?> GetCustomerAsync(string customerId, CancellationToken cancellationToken);

    bool VerifyWebhookSignature(string providedSecret);

    bool VerifyWithdrawalWebhookToken(string providedToken);
}
