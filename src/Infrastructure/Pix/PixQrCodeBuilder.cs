using System.Globalization;
using System.Text;
using Casamento.Application.Abstractions;
using QRCoder;

namespace Casamento.Infrastructure.Pix;

/// <summary>
/// Gera o payload EMV/BR Code do Pix (BACEN) e a imagem do QR Code.
/// Spec: Manual de Padrões para Iniciação do Pix — versão 2.x.
/// </summary>
internal sealed class PixQrCodeBuilder : IPixQrCodeBuilder
{
    public PixQrCodeResult Build(OwnPixInfo pix, decimal amount, string reference)
    {
        var payload = BuildEmvPayload(pix, amount, reference);
        var image = RenderQrPng(payload);
        return new PixQrCodeResult(payload, Convert.ToBase64String(image));
    }

    private static string BuildEmvPayload(OwnPixInfo pix, decimal amount, string reference)
    {
        var merchantAccountInfo =
            Tlv("00", "BR.GOV.BCB.PIX") +
            Tlv("01", pix.Key);

        var additionalData = Tlv("05", Sanitize(reference, 25, alphanumericOnly: true));

        var amountStr = amount.ToString("0.00", CultureInfo.InvariantCulture);
        var merchantName = Sanitize(pix.Beneficiary, 25);
        var city = Sanitize(string.IsNullOrWhiteSpace(pix.City) ? "SAO PAULO" : pix.City, 15);

        var withoutCrc =
            Tlv("00", "01") +                       // Payload Format Indicator
            Tlv("26", merchantAccountInfo) +        // Merchant Account Info (Pix)
            Tlv("52", "0000") +                     // Merchant Category Code
            Tlv("53", "986") +                      // Currency BRL
            Tlv("54", amountStr) +                  // Amount
            Tlv("58", "BR") +                       // Country
            Tlv("59", merchantName) +               // Merchant Name
            Tlv("60", city) +                       // City
            Tlv("62", additionalData) +             // Additional Data
            "6304";                                  // CRC field marker

        var crc = ComputeCrc16(withoutCrc);
        return withoutCrc + crc;
    }

    private static string Tlv(string id, string value)
    {
        var len = value.Length.ToString("D2", CultureInfo.InvariantCulture);
        return $"{id}{len}{value}";
    }

    private static string Sanitize(string value, int maxLength, bool alphanumericOnly = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "NA";
        }

        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (alphanumericOnly)
        {
            normalized = new string(normalized.Where(c => char.IsLetterOrDigit(c)).ToArray());
        }
        else
        {
            normalized = new string(normalized.Where(c => !char.IsControl(c)).ToArray());
        }

        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength];
        }

        return string.IsNullOrWhiteSpace(normalized) ? "NA" : normalized;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string ComputeCrc16(string data)
    {
        const ushort polynomial = 0x1021;
        ushort crc = 0xFFFF;
        var bytes = Encoding.UTF8.GetBytes(data);
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ polynomial)
                    : (ushort)(crc << 1);
            }
        }
        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }

    private static byte[] RenderQrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(pixelsPerModule: 6);
    }
}
