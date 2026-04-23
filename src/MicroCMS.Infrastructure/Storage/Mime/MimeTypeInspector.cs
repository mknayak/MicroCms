using MicroCMS.Application.Common.Interfaces;

namespace MicroCMS.Infrastructure.Storage.Mime;

/// <summary>
/// Detects the true MIME type by reading the first 512 bytes (magic bytes) of a stream.
/// Falls back to extension-based mapping when no known signature matches.
/// The stream position is restored after the probe so callers can re-read from the beginning.
/// </summary>
public sealed class MimeTypeInspector : IMimeTypeInspector
{
    private const int ProbeSize = 512;

    // Dictionary-based extension → MIME lookup keeps GetMimeFromExtension complexity low.
    private static readonly Dictionary<string, string> ExtensionMap =
 new(StringComparer.OrdinalIgnoreCase)
    {
     [".jpg"]  = "image/jpeg",
         [".jpeg"] = "image/jpeg",
 [".png"]  = "image/png",
            [".gif"]  = "image/gif",
   [".webp"] = "image/webp",
            [".svg"]  = "image/svg+xml",
       [".avif"] = "image/avif",
            [".bmp"]  = "image/bmp",
    [".tif"]  = "image/tiff",
     [".tiff"] = "image/tiff",
            [".mp4"]  = "video/mp4",
       [".mov"]  = "video/quicktime",
            [".avi"]  = "video/x-msvideo",
         [".webm"] = "video/webm",
    [".mp3"]  = "audio/mpeg",
            [".wav"]  = "audio/wav",
      [".ogg"]  = "audio/ogg",
  [".pdf"]  = "application/pdf",
      [".doc"]  = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
         [".xls"]  = "application/vnd.ms-excel",
   [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  [".ppt"]  = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
     [".zip"]  = "application/zip",
          [".tar"]  = "application/x-tar",
       [".gz"]   = "application/gzip",
        };

 public async Task<string> DetectAsync(
        Stream content,
        string fallbackFileName,
        CancellationToken cancellationToken = default)
    {
        var probe = new byte[ProbeSize];
        var startPosition = content.CanSeek ? content.Position : 0L;

        var bytesRead = await content.ReadAsync(probe.AsMemory(0, ProbeSize), cancellationToken);

        if (content.CanSeek)
         content.Seek(startPosition, SeekOrigin.Begin);

        var detected = DetectFromBytes(probe, bytesRead);
        return detected ?? GetMimeFromExtension(fallbackFileName) ?? "application/octet-stream";
}

    // ── Magic-byte detection (split into sub-methods to keep complexity low) ─

    private static string? DetectFromBytes(byte[] bytes, int length)
    {
   if (length < 4) return null;

        return DetectImageMime(bytes, length)
 ?? DetectDocumentMime(bytes)
?? DetectAudioVideoMime(bytes, length);
    }

    private static string? DetectImageMime(byte[] bytes, int length)
    {
        return DetectJpegPngGif(bytes, length)
         ?? DetectWebPBmpTiff(bytes, length);
    }

    private static string? DetectJpegPngGif(byte[] bytes, int length)
    {
    if (IsJpeg(bytes)) return "image/jpeg";
if (length >= 8 && IsPng(bytes)) return "image/png";
   if (IsGif(bytes)) return "image/gif";
 return null;
    }

    private static bool IsJpeg(byte[] bytes) =>
   bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

    private static bool IsPng(byte[] bytes) =>
        bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;

    private static bool IsGif(byte[] bytes) =>
        bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38;

    private static string? DetectWebPBmpTiff(byte[] bytes, int length)
    {
   if (length >= 12 && IsRiffWebP(bytes)) return "image/webp";
    if (IsBmp(bytes)) return "image/bmp";
     if (IsTiff(bytes)) return "image/tiff";
        return null;
    }

    private static bool IsRiffWebP(byte[] bytes) =>
        bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
        && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;

    private static bool IsBmp(byte[] bytes) =>
        bytes[0] == 0x42 && bytes[1] == 0x4D;

    private static bool IsTiff(byte[] bytes) =>
    (bytes[0] == 0x49 && bytes[1] == 0x49) || (bytes[0] == 0x4D && bytes[1] == 0x4D);

    private static string? DetectDocumentMime(byte[] bytes)
    {
 // PDF: 25 50 44 46
        if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
            return "application/pdf";

        // ZIP / DOCX / XLSX / PPTX: 50 4B 03 04
        if (bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
      return "application/zip";

     return null;
 }

    private static string? DetectAudioVideoMime(byte[] bytes, int length)
    {
        // MP4 / MOV: ftyp box at offset 4
        if (length >= 8 && bytes[4] == 0x66 && bytes[5] == 0x74
   && bytes[6] == 0x79 && bytes[7] == 0x70)
   return "video/mp4";

        // MP3: ID3 tag or sync word
        if (IsMp3(bytes))
   return "audio/mpeg";

        return null;
    }

    private static bool IsMp3(byte[] bytes) =>
        (bytes[0] == 0x49 && bytes[1] == 0x44 && bytes[2] == 0x33)
        || (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0);

  // ── Extension fallback ────────────────────────────────────────────────

    private static string? GetMimeFromExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return ExtensionMap.TryGetValue(ext, out var mime) ? mime : null;
    }
}
