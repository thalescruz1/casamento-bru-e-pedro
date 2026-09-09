namespace Casamento.Application.Abstractions;

public sealed record UploadGiftImageResult(string BlobName);

public interface IGiftImageStorage
{
    Task<UploadGiftImageResult> UploadAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken);

    string BuildPublicUrl(string blobName, TimeSpan lifetime);
}
