using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Ai.DraftGeneration;

[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record GenerateDraftCommand : ICommand<GeneratedDraftDto>
{
    public required Guid SiteId { get; init; }
    public required Guid ContentTypeId { get; init; }
    public required string Prompt { get; init; }
    public Dictionary<string, object>? Context { get; init; }
}

public sealed class GeneratedDraftDto
{
    public required Dictionary<string, object> Fields { get; init; }
    public required string Title { get; init; }
    public string? Slug { get; init; }
    public int TokensUsed { get; init; }
}
