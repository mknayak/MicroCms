namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>MediaAsset</c> entities.</summary>
public readonly record struct MediaAssetId(Guid Value)
{
    public static MediaAssetId New() => new(Guid.NewGuid());
    public static MediaAssetId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static MediaAssetId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out MediaAssetId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new MediaAssetId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
