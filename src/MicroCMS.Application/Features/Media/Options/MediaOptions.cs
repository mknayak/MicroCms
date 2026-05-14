namespace MicroCMS.Application.Features.Media.Options;

/// <summary>
/// Configuration options for the Media Library feature.
/// Bound from the <c>MicroCMS:Media</c> configuration section.
/// </summary>
public sealed class MediaOptions
{
    public const string SectionName = "MicroCMS:Media";

    /// <summary>
    /// When <c>true</c> the upload handler marks assets <c>Available</c> immediately
    /// without waiting for the background <c>MediaScanJob</c>.
    /// Set to <c>true</c> in environments where ClamAV is not running
    /// (<c>ClamAv:Enabled: false</c>) so files never get stuck in <c>PendingScan</c>.
    /// Defaults to <c>false</c> so production environments require an explicit opt-in.
    /// </summary>
    public bool SkipVirusScan { get; set; } = false;

    /// <summary>
    /// Lower-cased file extensions (including the leading dot) that are permitted
    /// for upload.
    /// </summary>
    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".svg",
        ".mp4", ".mov", ".avi", ".webm",
        ".mp3", ".wav", ".ogg",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".zip", ".tar", ".gz",
        ".js", ".mjs", ".css",
        ".html", ".htm", ".xml", ".json", ".txt",
        ".woff", ".woff2", ".ttf", ".otf"
    ];

    /// <summary>
    /// Extension-to-MIME fallback map used when magic-byte detection yields no result.
    /// Keys must include the leading dot and are matched case-insensitively.
    /// </summary>
    public Dictionary<string, string> ExtensionMimeTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"]   = "image/jpeg",
        [".jpeg"]  = "image/jpeg",
        [".png"]   = "image/png",
        [".gif"]   = "image/gif",
        [".webp"]  = "image/webp",
        [".svg"]   = "image/svg+xml",
        [".avif"]  = "image/avif",
        [".bmp"]   = "image/bmp",
        [".tif"]   = "image/tiff",
        [".tiff"]  = "image/tiff",
        [".mp4"]   = "video/mp4",
        [".mov"]   = "video/quicktime",
        [".avi"]   = "video/x-msvideo",
        [".webm"]  = "video/webm",
        [".mp3"]   = "audio/mpeg",
        [".wav"]   = "audio/wav",
        [".ogg"]   = "audio/ogg",
        [".pdf"]   = "application/pdf",
        [".doc"]   = "application/msword",
        [".docx"]  = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"]   = "application/vnd.ms-excel",
        [".xlsx"]  = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"]   = "application/vnd.ms-powerpoint",
        [".pptx"]  = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".zip"]   = "application/zip",
        [".tar"]   = "application/x-tar",
        [".gz"]    = "application/gzip",
        [".js"]    = "text/javascript",
        [".mjs"]   = "text/javascript",
        [".css"]   = "text/css",
        [".html"]  = "text/html",
        [".htm"]   = "text/html",
        [".xml"]   = "application/xml",
        [".json"]  = "application/json",
        [".txt"]   = "text/plain",
        [".woff"]  = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"]   = "font/ttf",
        [".otf"]   = "font/otf",
    };
}
