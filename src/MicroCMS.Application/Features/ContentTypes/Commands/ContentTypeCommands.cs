using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.ContentTypes.Commands;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record CreateContentTypeCommand(
    Guid SiteId,
    string Handle,
    string DisplayName,
    string? Description = null,
    LocalizationMode Localization = LocalizationMode.PerLocale,
    string Kind = "Content") : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record AddFieldCommand(
    Guid ContentTypeId,
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false,
    bool IsUnique = false,
    bool IsIndexed = false,
    bool IsList = false,
    string? Description = null,
    /// <summary>Static options for Enum fields. Ignored when DynamicSource is set.</summary>
    IReadOnlyList<string>? Options = null,
    /// <summary>Dynamic source config for Enum fields.</summary>
    FieldDynamicSourceInput? DynamicSource = null) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record RemoveFieldCommand(
    Guid ContentTypeId,
    Guid FieldId) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record PublishContentTypeCommand(Guid ContentTypeId) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record ArchiveContentTypeCommand(Guid ContentTypeId) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record UpdateContentTypeCommand(
    Guid ContentTypeId,
    string DisplayName,
    string? Description = null,
    LocalizationMode? Localization = null,
    string? Kind = null,
    Guid? LayoutId = null,
    IReadOnlyList<UpdateFieldInput>? Fields = null) : ICommand<ContentTypeDto>;

/// <summary>
/// Represents a field in the full-update payload.
/// <para>
/// <c>Id == null</c> → add as a new field.<br/>
/// <c>Id != null</c> → update the existing field with that ID.<br/>
/// Fields currently on the content type whose ID is absent from the list are removed.
/// </para>
/// </summary>
public sealed record UpdateFieldInput(
    Guid? Id,
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false,
    bool IsUnique = false,
    bool IsIndexed = false,
    bool IsList = false,
    int SortOrder = 0,
    string? Description = null,
    /// <summary>Static options for Enum fields.</summary>
    IReadOnlyList<string>? Options = null,
    /// <summary>Dynamic source config for Enum fields.</summary>
    FieldDynamicSourceInput? DynamicSource = null);

/// <summary>
/// DTO for specifying a dynamic entry source for Enum fields in commands.
/// Mirrors <see cref="MicroCMS.Domain.Aggregates.Content.FieldDynamicSource"/>.
/// </summary>
public sealed record FieldDynamicSourceInput(
    string ContentTypeHandle,
    string LabelField = "title",
    string ValueField = "slug",
    string StatusFilter = "Published");

/// <summary>Imports a ContentType schema from a JSON Schema document (BE-07c).</summary>
[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record ImportContentTypeSchemaCommand(
  Guid SiteId,
    string Handle,
    string DisplayName,
    string? Description,
    IReadOnlyList<ImportFieldInput> Fields) : ICommand<ContentTypeDto>;

public sealed record ImportFieldInput(
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false);

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record DeleteContentTypeCommand(Guid ContentTypeId) : ICommand;

/// <summary>Sets or clears the layout associated with a Page-kind content type.</summary>
[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record SetContentTypeLayoutCommand(
    Guid ContentTypeId,
    Guid? LayoutId) : ICommand<ContentTypeDto>;
