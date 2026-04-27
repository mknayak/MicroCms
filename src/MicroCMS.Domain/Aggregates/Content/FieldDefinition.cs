using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Content;

/// <summary>
/// Defines a single field within a <see cref="ContentType"/> schema.
/// Handle is a machine-readable name; label is displayed in the admin UI.
/// </summary>
public sealed class FieldDefinition : Entity<Guid>
{
    public const int MaxHandleLength = 64;
    public const int MaxLabelLength = 200;
    public const int MaxDescriptionLength = 500;

    private FieldDefinition() { } // EF Core

    private FieldDefinition(
        Guid id,
        ContentTypeId contentTypeId,
        string handle,
        string label,
        FieldType fieldType,
        bool isRequired,
        bool isLocalized,
        bool isUnique,
        int sortOrder,
        string? description,
        string? validationJson)
        : base(id)
    {
        ContentTypeId = contentTypeId;
        Handle = handle;
        Label = label;
        FieldType = fieldType;
        IsRequired = isRequired;
        IsLocalized = isLocalized;
        IsUnique = isUnique;
        SortOrder = sortOrder;
        Description = description;
        ValidationJson = validationJson;
    }

    public ContentTypeId ContentTypeId { get; private set; }
    public string Handle { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public FieldType FieldType { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsLocalized { get; private set; }
    public bool IsUnique { get; private set; }
    public int SortOrder { get; private set; }
    public string? Description { get; private set; }

    /// <summary>JSON-encoded field-type-specific validation config.</summary>
    public string? ValidationJson { get; private set; }

    internal static FieldDefinition Create(
        ContentTypeId contentTypeId,
        string handle,
        string label,
        FieldType fieldType,
        bool isRequired,
        bool isLocalized,
        bool isUnique,
        int sortOrder,
        string? description = null,
        string? validationJson = null)
    {
        ValidateHandle(handle);
        ValidateLabel(label);

        return new FieldDefinition(
            Guid.NewGuid(),
            contentTypeId,
            handle.Trim(),
            label.Trim(),
            fieldType,
            isRequired,
            isLocalized,
            isUnique,
            sortOrder,
            description?.Trim(),
            validationJson);
    }

    internal void Update(string label, FieldType fieldType, bool isRequired, bool isLocalized, int sortOrder, string? description)
    {
        ValidateLabel(label);
        Label = label.Trim();
        FieldType = fieldType;
        IsRequired = isRequired;
        IsLocalized = isLocalized;
        SortOrder = sortOrder;
        Description = description?.Trim();
    }

    private static void ValidateHandle(string handle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handle, nameof(handle));

        if (handle.Length > MaxHandleLength)
        {
            throw new DomainException($"Field handle must not exceed {MaxHandleLength} characters.");
        }

        if (!handle.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            throw new DomainException(
                "Field handle may only contain letters, digits, and underscores.");
        }
    }

    private static void ValidateLabel(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label, nameof(label));

        if (label.Length > MaxLabelLength)
        {
            throw new DomainException($"Field label must not exceed {MaxLabelLength} characters.");
        }
    }
}
