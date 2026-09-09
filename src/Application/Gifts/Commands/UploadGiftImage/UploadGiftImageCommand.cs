using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Gifts.Dtos;
using Casamento.Application.Gifts.Mapping;
using FluentValidation;
using MediatR;

namespace Casamento.Application.Gifts.Commands.UploadGiftImage;

public sealed record UploadGiftImageCommand(
    string FileName,
    string ContentType,
    Stream Content) : IRequest<Result<UploadedImageDto>>;

public sealed class UploadGiftImageValidator : AbstractValidator<UploadGiftImageCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public UploadGiftImageValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(AllowedContentTypes.Contains)
            .WithMessage("Formato não suportado. Use JPEG, PNG ou WebP.");
        RuleFor(x => x.Content).NotNull();
    }
}

public sealed class UploadGiftImageHandler(IGiftImageStorage storage)
    : IRequestHandler<UploadGiftImageCommand, Result<UploadedImageDto>>
{
    public async Task<Result<UploadedImageDto>> Handle(
        UploadGiftImageCommand request,
        CancellationToken cancellationToken)
    {
        var result = await storage.UploadAsync(request.FileName, request.ContentType, request.Content, cancellationToken)
            .ConfigureAwait(false);

        var url = storage.BuildPublicUrl(result.BlobName, GiftMapping.SasLifetime);
        return new UploadedImageDto(result.BlobName, url);
    }
}
