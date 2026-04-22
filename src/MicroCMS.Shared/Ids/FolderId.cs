namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>Folder</c> entities.</summary>
public readonly record struct FolderId(Guid Value)
{
    public static FolderId New() => new(Guid.NewGuid());
  public static FolderId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static FolderId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out FolderId result)
{
        if (Guid.TryParse(value, out var guid))
        {
          result = new FolderId(guid);
            return true;
        }

        result = Empty;
  return false;
    }
}
