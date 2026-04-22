using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Application.Features.Media.Mappers;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Ai.AltText;

/// <summary>
/// Generates AI alt text for a media asset and stores it on the aggregate (GAP-14).
/// Uses a vision-capable LLM; the storage key is passed as context.
/// </summary>
[HasPolicy(ContentPolicies.MediaUpload)]
public sealed record GenerateAltTextCommand(Guid AssetId) : ICommand<MediaAssetDto>;

/// <summary>Handles <see cref="GenerateAltTextCommand"/>.</summary>
public sealed class GenerateAltTextCommandHandler(
    IRepository<MediaAsset, MediaAssetId> assetRepository,
  ILlmService llm)
    : IRequestHandler<GenerateAltTextCommand, Result<MediaAssetDto>>
{
    public async Task<Result<MediaAssetDto>> Handle(
  GenerateAltTextCommand request,
        CancellationToken cancellationToken)
    {
   var asset = await assetRepository.GetByIdAsync(new MediaAssetId(request.AssetId), cancellationToken)
  ?? throw new NotFoundException(nameof(MediaAsset), request.AssetId);

        var llmRequest = new LlmRequest(
     SystemPrompt: "You are an accessibility expert. Generate concise, descriptive alt text for the image. " +
           "Return only the alt text string, 10–125 characters.",
            UserMessage: $"Image filename: {asset.Metadata.FileName}. Storage key: {asset.StorageKey}.",
     FeatureHint: "alt_text");

        var response = await llm.CompleteAsync(llmRequest, cancellationToken);
     asset.SetAiAltText(response.Content.Trim());
   assetRepository.Update(asset);

  return Result.Success(MediaMapper.ToDto(asset));
    }
}
