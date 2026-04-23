namespace MicroCMS.Infrastructure.Storage.AzureBlob;

/// <summary>Configuration for the Azure Blob Storage provider.</summary>
public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "Storage:AzureBlob";

    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "microcms-media";

    /// <summary>When true, <see cref="AzureBlobStorageProvider.GetPublicUrlAsync"/> returns the blob URI.</summary>
    public bool PublicAccess { get; set; } = false;
}
