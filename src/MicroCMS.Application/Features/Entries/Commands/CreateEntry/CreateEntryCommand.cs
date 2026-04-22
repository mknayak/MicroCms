using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Commands.CreateEntry;

/// <summary>
/// Creates a new content entry in Draft status.
/// The command carries raw string values for Slug and Locale;
/// value-object construction occurs inside the handler after validation passes.
/// </summary>
[HasPolicy(ContentPolicies.EntryCreate)]
public sealed record CreateEntryCommand(
    Guid SiteId,
    Guid ContentTypeId,
    string Slug,
    string Locale,
    string FieldsJson = "{}") : ICommand<EntryDto>;
