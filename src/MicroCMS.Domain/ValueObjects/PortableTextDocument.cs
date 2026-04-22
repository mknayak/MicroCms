using System.Text.Json;
using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// Represents a Portable Text document — a structured JSON array of block nodes (GAP-03).
/// Portable Text is the interchange format produced by the TipTap editor.
/// The domain stores the serialised JSON; validation ensures the root is a JSON array.
/// Rich Text field values on entries should be stored in this format.
/// </summary>
public sealed class PortableTextDocument : ValueObject
{
    /// <summary>The raw JSON string representing the array of Portable Text blocks.</summary>
    public string Json { get; private set; } = "[]";

    private PortableTextDocument() { } // EF Core

    private PortableTextDocument(string json)
    {
        Json = json;
    }

  /// <summary>
    /// Creates a <see cref="PortableTextDocument"/> from a validated JSON string.
    /// Throws <see cref="DomainException"/> if the JSON is not a valid array.
    /// </summary>
    public static PortableTextDocument Create(string json)
    {
    ArgumentException.ThrowIfNullOrWhiteSpace(json, nameof(json));

        try
   {
       using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new DomainException("Portable Text document must be a JSON array of blocks.");
        }
        catch (JsonException ex)
     {
     throw new DomainException($"Portable Text document contains invalid JSON: {ex.Message}");
  }

        return new PortableTextDocument(json);
    }

    /// <summary>An empty Portable Text document (empty array).</summary>
    public static PortableTextDocument Empty => new("[]");

    /// <summary>Returns true when the document has no blocks.</summary>
    public bool IsEmpty => Json == "[]" || Json == "[ ]";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Json;
    }
}
