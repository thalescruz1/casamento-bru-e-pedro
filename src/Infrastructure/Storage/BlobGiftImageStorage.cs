using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Casamento.Application.Abstractions;
using Casamento.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Casamento.Infrastructure.Storage;

internal sealed class BlobGiftImageStorage(
    BlobServiceClient blobServiceClient,
    IOptions<BlobOptions> options,
    ILogger<BlobGiftImageStorage> logger) : IGiftImageStorage
{
    private const int MaxDimension = 1200;
    private const int JpegQuality = 82;

    private readonly BlobOptions _options = options.Value;
    private readonly BlobContainerClient _container = blobServiceClient.GetBlobContainerClient(options.Value.GiftImagesContainer);

    public async Task<UploadGiftImageResult> UploadAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await using var processed = new MemoryStream();
        using (var image = await Image.LoadAsync(content, cancellationToken).ConfigureAwait(false))
        {
            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimension, MaxDimension)
                }));
            }

            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            await image.SaveAsync(processed, new JpegEncoder { Quality = JpegQuality }, cancellationToken)
                .ConfigureAwait(false);
        }

        processed.Position = 0;

        var blobName = $"{GuidV7.NewGuid():N}.jpg";
        var blob = _container.GetBlobClient(blobName);

        await blob.UploadAsync(processed, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "image/jpeg",
                CacheControl = "public, max-age=31536000, immutable"
            }
        }, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Imagem {BlobName} enviada ({Bytes} bytes)", blobName, processed.Length);
        return new UploadGiftImageResult(blobName);
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        await _container.GetBlobClient(blobName)
            .DeleteIfExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public string BuildPublicUrl(string blobName, TimeSpan lifetime)
    {
        var blob = _container.GetBlobClient(blobName);

        if (!blob.CanGenerateSasUri)
        {
            return blob.Uri.ToString();
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _container.Name,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(lifetime)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blob.GenerateSasUri(sasBuilder).ToString();
    }
}
