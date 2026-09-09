namespace Casamento.Application.Abstractions;

public sealed record PixQrCodeResult(string Payload, string ImageBase64);

public interface IPixQrCodeBuilder
{
    PixQrCodeResult Build(OwnPixInfo pix, decimal amount, string reference);
}
