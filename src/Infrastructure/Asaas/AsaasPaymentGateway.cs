using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Casamento.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Asaas;

internal sealed class AsaasPaymentGateway(
    HttpClient httpClient,
    IOptions<AsaasOptions> options,
    ILogger<AsaasPaymentGateway> logger) : IPaymentGateway
{
    private readonly AsaasOptions _options = options.Value;

    public async Task<PaymentPixResult> CreatePixPaymentAsync(CreatePixPaymentRequest request, CancellationToken cancellationToken)
    {
        var customerId = await EnsureCustomerAsync(request.GuestName, request.GuestEmail, request.GuestDocument, cancellationToken)
            .ConfigureAwait(false);

        var payload = new AsaasCreatePaymentRequest
        {
            Customer = customerId,
            BillingType = "PIX",
            Value = request.AmountBrl,
            DueDate = request.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Description = request.Description,
            ExternalReference = request.GiftId.ToString("N"),
            PostalService = false
        };

        var payment = await PostPaymentAsync(payload, cancellationToken).ConfigureAwait(false);

        using var qrResponse = await httpClient.GetAsync(
            new Uri($"payments/{Uri.EscapeDataString(payment.Id)}/pixQrCode", UriKind.Relative),
            cancellationToken).ConfigureAwait(false);

        if (!qrResponse.IsSuccessStatusCode)
        {
            var errorBody = await qrResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("Asaas recusou QR code Pix ({Status}): {Body}", qrResponse.StatusCode, errorBody);
            throw new HttpRequestException($"Asaas {(int)qrResponse.StatusCode}: {errorBody}");
        }

        var qr = await qrResponse.Content.ReadFromJsonAsync<AsaasPixQrCodeResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Asaas retornou corpo vazio para QR code Pix.");

        return new PaymentPixResult(
            payment.Id,
            qr.EncodedImage ?? string.Empty,
            qr.Payload ?? string.Empty,
            qr.ExpirationDate);
    }

    public async Task<PaymentCardResult> CreateCardPaymentAsync(CreateCardPaymentRequest request, CancellationToken cancellationToken)
    {
        var customerId = await EnsureCustomerAsync(request.GuestName, request.GuestEmail, request.GuestDocument, cancellationToken)
            .ConfigureAwait(false);

        var normalizedDocument = new string(request.GuestDocument.Where(char.IsDigit).ToArray());
        var normalizedPostal = new string(request.HolderInfo.PostalCode.Where(char.IsDigit).ToArray());
        var normalizedPhone = new string(request.HolderInfo.Phone.Where(char.IsDigit).ToArray());
        var normalizedCard = new string(request.Card.Number.Where(char.IsDigit).ToArray());

        var payload = new AsaasCreatePaymentRequest
        {
            Customer = customerId,
            BillingType = "CREDIT_CARD",
            Value = request.AmountBrl,
            DueDate = request.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Description = request.Description,
            ExternalReference = request.GiftId.ToString("N"),
            PostalService = false,
            RemoteIp = string.IsNullOrWhiteSpace(request.RemoteIp) ? null : request.RemoteIp,
            CreditCard = new AsaasCreditCard
            {
                HolderName = request.Card.HolderName,
                Number = normalizedCard,
                ExpiryMonth = request.Card.ExpiryMonth.PadLeft(2, '0'),
                ExpiryYear = request.Card.ExpiryYear,
                Ccv = request.Card.Ccv
            },
            CreditCardHolderInfo = new AsaasCreditCardHolderInfo
            {
                Name = request.GuestName,
                Email = request.GuestEmail,
                CpfCnpj = normalizedDocument,
                PostalCode = normalizedPostal,
                AddressNumber = request.HolderInfo.AddressNumber,
                Phone = normalizedPhone
            }
        };

        var payment = await PostPaymentAsync(payload, cancellationToken).ConfigureAwait(false);

        var status = payment.Status?.ToUpperInvariant() switch
        {
            "CONFIRMED" or "RECEIVED" or "RECEIVED_IN_CASH" => PaymentCardStatus.Confirmed,
            "PENDING" or "AUTHORIZED" => PaymentCardStatus.Pending,
            _ => PaymentCardStatus.Refused
        };

        return new PaymentCardResult(payment.Id, status, payment.InvoiceUrl);
    }

    public async Task<PaymentCustomer?> GetCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        using var response = await httpClient.GetAsync(
            new Uri($"customers/{Uri.EscapeDataString(customerId)}", UriKind.Relative),
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Asaas GET customer {CustomerId} retornou {Status}", customerId, response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<AsaasCustomerDetail>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (body is null || string.IsNullOrWhiteSpace(body.Email))
        {
            return null;
        }

        return new PaymentCustomer(body.Name ?? string.Empty, body.Email);
    }

    public bool VerifyWebhookSignature(string providedSecret)
    {
        if (string.IsNullOrEmpty(providedSecret))
        {
            return false;
        }

        var expected = Encoding.UTF8.GetBytes(_options.WebhookSecret);
        var provided = Encoding.UTF8.GetBytes(providedSecret);
        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    public bool VerifyWithdrawalWebhookToken(string providedToken)
    {
        if (string.IsNullOrEmpty(providedToken) || string.IsNullOrEmpty(_options.WithdrawalWebhookToken))
        {
            return false;
        }

        var expected = Encoding.UTF8.GetBytes(_options.WithdrawalWebhookToken);
        var provided = Encoding.UTF8.GetBytes(providedToken);
        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    private async Task<AsaasPaymentResponse> PostPaymentAsync(AsaasCreatePaymentRequest payload, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("payments", payload, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("Asaas recusou criar pagamento ({Status}): {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Asaas {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<AsaasPaymentResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Asaas retornou corpo vazio para criar pagamento.");

        logger.LogInformation("Asaas payment {PaymentId} criado (status={Status}) para {ExternalRef}",
            result.Id, result.Status, payload.ExternalReference);
        return result;
    }

    private async Task<string> EnsureCustomerAsync(string name, string email, string document, CancellationToken cancellationToken)
    {
        var normalizedDocument = new string(document.Where(char.IsDigit).ToArray());

        using var lookup = await httpClient.GetAsync(
            new Uri($"customers?cpfCnpj={Uri.EscapeDataString(normalizedDocument)}", UriKind.Relative),
            cancellationToken).ConfigureAwait(false);

        if (lookup.IsSuccessStatusCode)
        {
            var page = await lookup.Content.ReadFromJsonAsync<AsaasListResponse<AsaasCustomer>>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var existing = page?.Data?.FirstOrDefault();
            if (existing is not null)
            {
                return existing.Id;
            }
        }

        using var created = await httpClient.PostAsJsonAsync("customers", new AsaasCreateCustomerRequest
        {
            Name = name,
            Email = email,
            CpfCnpj = normalizedDocument,
            NotificationDisabled = true
        }, cancellationToken).ConfigureAwait(false);

        if (!created.IsSuccessStatusCode)
        {
            var errorBody = await created.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("Asaas recusou criar cliente ({Status}): {Body}", created.StatusCode, errorBody);
            throw new HttpRequestException($"Asaas {(int)created.StatusCode}: {errorBody}");
        }

        var customer = await created.Content.ReadFromJsonAsync<AsaasCustomer>(cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Asaas retornou corpo vazio ao criar cliente.");
        return customer.Id;
    }
}

internal sealed class AsaasCreateCustomerRequest
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("cpfCnpj")] public string CpfCnpj { get; set; } = string.Empty;
    [JsonPropertyName("notificationDisabled")] public bool NotificationDisabled { get; set; }
}

internal sealed class AsaasCreatePaymentRequest
{
    [JsonPropertyName("customer")] public string Customer { get; set; } = string.Empty;
    [JsonPropertyName("billingType")] public string BillingType { get; set; } = "UNDEFINED";
    [JsonPropertyName("value")] public decimal Value { get; set; }
    [JsonPropertyName("dueDate")] public string DueDate { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("externalReference")] public string ExternalReference { get; set; } = string.Empty;
    [JsonPropertyName("postalService")] public bool PostalService { get; set; }

    [JsonPropertyName("remoteIp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RemoteIp { get; set; }

    [JsonPropertyName("creditCard")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AsaasCreditCard? CreditCard { get; set; }

    [JsonPropertyName("creditCardHolderInfo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AsaasCreditCardHolderInfo? CreditCardHolderInfo { get; set; }
}

internal sealed class AsaasCreditCard
{
    [JsonPropertyName("holderName")] public string HolderName { get; set; } = string.Empty;
    [JsonPropertyName("number")] public string Number { get; set; } = string.Empty;
    [JsonPropertyName("expiryMonth")] public string ExpiryMonth { get; set; } = string.Empty;
    [JsonPropertyName("expiryYear")] public string ExpiryYear { get; set; } = string.Empty;
    [JsonPropertyName("ccv")] public string Ccv { get; set; } = string.Empty;
}

internal sealed class AsaasCreditCardHolderInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("cpfCnpj")] public string CpfCnpj { get; set; } = string.Empty;
    [JsonPropertyName("postalCode")] public string PostalCode { get; set; } = string.Empty;
    [JsonPropertyName("addressNumber")] public string AddressNumber { get; set; } = string.Empty;
    [JsonPropertyName("phone")] public string Phone { get; set; } = string.Empty;
}

internal sealed class AsaasPaymentResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("invoiceUrl")] public string? InvoiceUrl { get; set; }
}

internal sealed class AsaasPixQrCodeResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("encodedImage")] public string? EncodedImage { get; set; }
    [JsonPropertyName("payload")] public string? Payload { get; set; }
    [JsonPropertyName("expirationDate")] public DateTimeOffset ExpirationDate { get; set; }
}

internal sealed class AsaasCustomer
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
}

internal sealed class AsaasCustomerDetail
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
}

internal sealed class AsaasListResponse<T>
{
    [JsonPropertyName("data")] public List<T>? Data { get; set; }
}
