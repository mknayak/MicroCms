using FluentValidation;
using MicroCMS.Domain.ValueObjects;

namespace MicroCMS.Application.Features.Media.Commands;

public sealed class UploadMediaAssetCommandValidator : AbstractValidator<UploadMediaAssetCommand>
{
    private static readonly string[] AllowedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".svg",
        ".mp4", ".mov", ".avi", ".webm",
        ".mp3", ".wav", ".ogg",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".zip", ".tar", ".gz"
    ];

    public UploadMediaAssetCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(AssetMetadata.MaxFileNameLength)
            .Must(HaveAllowedExtension)
            .WithMessage("File type is not permitted.");

        RuleFor(x => x.ContentLength)
            .GreaterThan(0).WithMessage("File must not be empty.")
            .LessThanOrEqualTo(AssetMetadata.MaxFileSizeBytes)
            .WithMessage($"File exceeds the 2 GB maximum.");

        RuleFor(x => x.Content).NotNull();
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return Array.IndexOf(AllowedExtensions, ext) >= 0;
    }
}

public sealed class BulkMoveMediaCommandValidator : AbstractValidator<BulkMoveMediaCommand>
{
    public BulkMoveMediaCommandValidator()
    {
        RuleFor(x => x.AssetIds).NotEmpty().WithMessage("At least one asset ID is required.");
        RuleFor(x => x.AssetIds.Count).LessThanOrEqualTo(100).WithMessage("Maximum 100 assets per bulk operation.");
    }
}

public sealed class BulkDeleteMediaCommandValidator : AbstractValidator<BulkDeleteMediaCommand>
{
    public BulkDeleteMediaCommandValidator()
    {
        RuleFor(x => x.AssetIds).NotEmpty().WithMessage("At least one asset ID is required.");
        RuleFor(x => x.AssetIds.Count).LessThanOrEqualTo(100).WithMessage("Maximum 100 assets per bulk operation.");
    }
}

public sealed class BulkRetagMediaCommandValidator : AbstractValidator<BulkRetagMediaCommand>
{
    public BulkRetagMediaCommandValidator()
    {
        RuleFor(x => x.AssetIds).NotEmpty().WithMessage("At least one asset ID is required.");
        RuleFor(x => x.AssetIds.Count).LessThanOrEqualTo(100).WithMessage("Maximum 100 assets per bulk operation.");
        RuleFor(x => x.Tags).NotNull();
    }
}

public sealed class CreateMediaFolderCommandValidator : AbstractValidator<CreateMediaFolderCommand>
{
    public CreateMediaFolderCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class RenameMediaFolderCommandValidator : AbstractValidator<RenameMediaFolderCommand>
{
    public RenameMediaFolderCommandValidator()
    {
        RuleFor(x => x.FolderId).NotEmpty();
        RuleFor(x => x.NewName).NotEmpty().MaximumLength(200);
    }
}
