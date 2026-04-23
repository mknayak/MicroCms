namespace MicroCMS.Infrastructure.Storage.S3;

/// <summary>Configuration for the AWS S3 (or S3-compatible) storage provider.</summary>
public sealed class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>Service URL override — used for MinIO or other S3-compatible endpoints.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>When true, <see cref="S3StorageProvider.GetPublicUrlAsync"/> returns a plain HTTPS URL.</summary>
    public bool PublicAccess { get; set; } = false;
}
