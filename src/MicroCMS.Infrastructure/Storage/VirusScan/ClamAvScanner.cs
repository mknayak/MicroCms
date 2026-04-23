using System.Buffers;
using System.Net.Sockets;
using System.Text;
using MicroCMS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroCMS.Infrastructure.Storage.VirusScan;

/// <summary>
/// Sends the upload stream to a ClamAV daemon over TCP using the INSTREAM command.
/// Implements the nstream protocol: each chunk is prefixed with a 4-byte big-endian length,
/// followed by an empty (zero-length) chunk to signal end-of-stream.
/// </summary>
public sealed class ClamAvScanner : IClamAvScanner
{
    private const int ChunkSize = 65_536; // 64 KB chunks
    private const string OkResponse = "stream: OK";

    private readonly ClamAvOptions _options;
    private readonly ILogger<ClamAvScanner> _logger;

    public ClamAvScanner(IOptions<ClamAvOptions> options, ILogger<ClamAvScanner> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ScanInternalAsync(content, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "ClamAV scan failed — treating as unsafe.");
            return new ScanResult(IsClean: false, ThreatName: "ScanError: " + ex.Message);
        }
    }

    private async Task<ScanResult> ScanInternalAsync(Stream content, CancellationToken cancellationToken)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_options.Host, _options.Port, cancellationToken);

        await using var networkStream = tcp.GetStream();

        // Send INSTREAM command
        var command = Encoding.ASCII.GetBytes("zINSTREAM\0");
        await networkStream.WriteAsync(command, cancellationToken);

        // Stream content in chunks with 4-byte big-endian length prefix
        var buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
        try
        {
            int bytesRead;
            while ((bytesRead = await content.ReadAsync(buffer.AsMemory(0, ChunkSize), cancellationToken)) > 0)
            {
                await WriteChunkAsync(networkStream, buffer, bytesRead, cancellationToken);
            }

            // Terminate with zero-length chunk
            await WriteChunkAsync(networkStream, buffer, 0, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        // Read response
        var responseBuffer = new byte[1024];
        var responseLength = await networkStream.ReadAsync(responseBuffer, cancellationToken);
        var response = Encoding.ASCII.GetString(responseBuffer, 0, responseLength).Trim();

        _logger.LogDebug("ClamAV response: {Response}", response);

        if (response.EndsWith(OkResponse, StringComparison.OrdinalIgnoreCase))
            return new ScanResult(IsClean: true, ThreatName: null);

        // Response format: "stream: {THREAT_NAME} FOUND"
        var threatName = response.Replace("stream: ", string.Empty).Replace(" FOUND", string.Empty).Trim();
        return new ScanResult(IsClean: false, ThreatName: threatName);
    }

    private static async Task WriteChunkAsync(
        NetworkStream stream,
        byte[] data,
        int length,
        CancellationToken cancellationToken)
    {
        // Write 4-byte big-endian length prefix
        var header = new byte[4];
        header[0] = (byte)(length >> 24);
        header[1] = (byte)(length >> 16);
        header[2] = (byte)(length >> 8);
        header[3] = (byte)length;

        await stream.WriteAsync(header, cancellationToken);
        if (length > 0)
            await stream.WriteAsync(data.AsMemory(0, length), cancellationToken);
    }
}
