using HotChocolate;
using HotChocolate.Types;
using MicroCMS.Application.Features.ContentTypes.Dtos;

namespace MicroCMS.GraphQL.Schema;

/// <summary>
/// Service that translates <see cref="ContentTypeDto"/> field definitions into
/// Hot Chocolate field registrations at runtime.
///
/// Full per-content-type codegen (a separate GraphQL type per ContentType) is deferred to
/// Sprint 11 (TypeScript SDK) when the schema shape is consumed by generated SDK types.
/// For now the dynamic wrapper is sufficient for typed queries via the
/// <c>contentType</c> discriminator.
/// </summary>
public sealed class DynamicSchemaService
{
  // Dictionary-based mapping keeps cyclomatic complexity at 1 (CC ≤ 10 gate).
    private static readonly IReadOnlyDictionary<string, string> FieldTypeToScalar =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
 {
  { "TEXT",      "String" },
      { "RICHTEXT",  "String" },
  { "SLUG",      "String" },
         { "LOCALE",    "String" },
     { "URL",       "String" },
    { "EMAIL",     "String" },
{ "INTEGER",   "Int"    },
{ "INT",       "Int"    },
{ "FLOAT",     "Float"  },
   { "DECIMAL",   "Float"  },
      { "NUMBER",    "Float"  },
    { "BOOLEAN",   "Boolean" },
    { "BOOL",      "Boolean" },
            { "DATETIME",  "DateTime" },
      { "DATE",      "DateTime" },
     { "UUID",      "UUID"   },
          { "GUID",      "UUID" },
{ "JSON",    "JSON"   },
      { "OBJECT",    "JSON"   },
{ "ARRAY",     "JSON"   },
        };

    /// <summary>
    /// Maps a <see cref="FieldDefinitionDto.FieldType"/> string to the corresponding
  /// GraphQL scalar name used in the dynamic schema.
    /// Unknown types default to <c>"String"</c>.
    /// </summary>
  public static string MapFieldTypeToGraphQlScalar(string fieldType) =>
        FieldTypeToScalar.TryGetValue(fieldType, out var scalar) ? scalar : "String";
}
