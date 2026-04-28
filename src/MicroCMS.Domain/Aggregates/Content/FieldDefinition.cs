using System.Text.Json;
using System.Text.Json.Serialization;
using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Content;

/// <summary>
/// Typed validation/config stored as JSON in <see cref="FieldDefinition.ValidationJson"/>.
/// Used for Enum options (static list or dynamic entry query), string length constraints, etc.
/// </summary>
public sealed record FieldValidationConfig
{
  /// <summary>
    /// Static option list for Enum fields.
    /// Mutually exclusive with <see cref="DynamicSource"/>.
    /// </summary>
    [JsonPropertyName("options")]
    public IReadOnlyList<string>? Options { get; init; }

    /// <summary>
    /// Dynamic source config — resolves options from published entries of another content type.
    /// Mutually exclusive with <see cref="Options"/>.
    /// </summary>
    [JsonPropertyName("dynamicSource")]
    public FieldDynamicSource? DynamicSource { get; init; }

    /// <summary>Min length for ShortText / LongText fields.</summary>
    [JsonPropertyName("minLength")]
    public int? MinLength { get; init; }

    /// <summary>Max length for ShortText / LongText fields.</summary>
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; init; }

    /// <summary>Min value for Integer / Decimal fields.</summary>
    [JsonPropertyName("min")]
    public decimal? Min { get; init; }

    /// <summary>Max value for Integer / Decimal fields.</summary>
    [JsonPropertyName("max")]
    public decimal? Max { get; init; }

    private static readonly JsonSerializerOptions _opts =
        new() { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public string ToJson() => JsonSerializer.Serialize(this, _opts);

    public static FieldValidationConfig? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<FieldValidationConfig>(json, _opts); }
        catch (JsonException) { return null; }
    }
}

/// <summary>
/// Describes how to resolve Enum options dynamically from published entries.
/// </summary>
public sealed record FieldDynamicSource
{
    /// <summary>Handle of the content type whose entries supply the option list.</summary>
    [JsonPropertyName("contentTypeHandle")]
    public string ContentTypeHandle { get; init; } = string.Empty;

    /// <summary>Field handle on the source entry to use as the human-readable label.</summary>
    [JsonPropertyName("labelField")]
    public string LabelField { get; init; } = "title";

  /// <summary>Field handle on the source entry to use as the stored value.</summary>
    [JsonPropertyName("valueField")]
  public string ValueField { get; init; } = "slug";

    /// <summary>Optional status filter (defaults to "Published").</summary>
    [JsonPropertyName("statusFilter")]
  public string StatusFilter { get; init; } = "Published";
}

// ── FieldDefinition ──────────────────────────────────────────────────────────

/// <summary>
/// Defines a single field within a <see cref="ContentType"/> schema.
/// Handle is a machine-readable name; label is displayed in the admin UI.
/// When <see cref="IsList"/> is true the field stores a JSON array of values
/// rather than a single scalar.
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
        bool isIndexed,
        bool isList,
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
        IsIndexed = isIndexed;
        IsList = isList;
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
    public bool IsIndexed { get; private set; }
    public bool IsList { get; private set; }
    public int SortOrder { get; private set; }
  public string? Description { get; private set; }

    /// <summary>
    /// JSON-encoded <see cref="FieldValidationConfig"/> — stores static Enum options,
    /// dynamic source config, string length rules, numeric range rules, etc.
    /// </summary>
    public string? ValidationJson { get; private set; }

    /// <summary>Parsed validation config. Returns null when ValidationJson is absent/invalid.</summary>
    public FieldValidationConfig? Validation => FieldValidationConfig.FromJson(ValidationJson);

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
 string? validationJson = null,
        bool isIndexed = false,
        bool isList = false)
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
    isIndexed,
         isList,
sortOrder,
            description?.Trim(),
    validationJson);
    }

    internal void Update(
        string label,
        FieldType fieldType,
   bool isRequired,
   bool isLocalized,
        bool isIndexed,
     bool isList,
        int sortOrder,
    string? description,
        string? validationJson = null)
  {
        ValidateLabel(label);
   Label = label.Trim();
        FieldType = fieldType;
        IsRequired = isRequired;
        IsLocalized = isLocalized;
        IsIndexed = isIndexed;
   IsList = isList;
        SortOrder = sortOrder;
        Description = description?.Trim();
        if (validationJson is not null)
            ValidationJson = validationJson;
    }

    private static void ValidateHandle(string handle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handle, nameof(handle));
        if (handle.Length > MaxHandleLength)
  throw new DomainException($"Field handle must not exceed {MaxHandleLength} characters.");
        if (!handle.All(c => char.IsLetterOrDigit(c) || c == '_'))
   throw new DomainException("Field handle may only contain letters, digits, and underscores.");
    }

    private static void ValidateLabel(string label)
    {
    ArgumentException.ThrowIfNullOrWhiteSpace(label, nameof(label));
        if (label.Length > MaxLabelLength)
   throw new DomainException($"Field label must not exceed {MaxLabelLength} characters.");
    }
}
