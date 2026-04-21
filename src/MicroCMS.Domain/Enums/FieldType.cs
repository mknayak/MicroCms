namespace MicroCMS.Domain.Enums;

/// <summary>
/// Supported field types for content type schema definitions.
/// Maps to FR-CM-2 in the design document.
/// </summary>
public enum FieldType
{
    ShortText = 0,
    LongText = 1,
    RichText = 2,
    Markdown = 3,
    Integer = 4,
    Decimal = 5,
    Boolean = 6,
    DateTime = 7,
    Enum = 8,
    Reference = 9,
    AssetReference = 10,
    Json = 11,
    Component = 12,
    Location = 13,
    Color = 14
}
