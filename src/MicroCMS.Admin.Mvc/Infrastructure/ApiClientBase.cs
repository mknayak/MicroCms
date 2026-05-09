using System.Net.Http.Json;
using System.Text.Json;
using MicroCMS.Admin.Mvc.Extensions;

namespace MicroCMS.Admin.Mvc.Infrastructure;

/// <summary>
/// Base class for all API service implementations.
/// Provides strongly-typed HTTP verb helpers that map non-2xx responses to
/// <see cref="ApiException"/> and deserialise successful response bodies.
/// </summary>
public abstract class ApiClientBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;

    protected ApiClientBase(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected HttpClient CreateClient() =>
        _httpClientFactory.CreateClient(HttpClientNames.ApiClient);

    protected async Task<T> GetAsync<T>(string path, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync(path, ct);
        return await ReadResponseAsync<T>(response, ct);
    }

    protected async Task<T> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(path, body, JsonOptions, ct);
        return await ReadResponseAsync<T>(response, ct);
    }

    protected async Task PostAsync(string path, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
    }

    protected async Task<T> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.PutAsJsonAsync(path, body, JsonOptions, ct);
        return await ReadResponseAsync<T>(response, ct);
    }

    protected async Task PutAsync(string path, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.PutAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
    }

    protected async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.DeleteAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
    }

    protected async Task<byte[]> GetBytesAsync(string path, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    protected async Task<T> PostMultipartAsync<T>(string path, MultipartFormDataContent form, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.PostAsync(path, form, ct);
        return await ReadResponseAsync<T>(response, ct);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await EnsureSuccessAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return result ?? throw new ApiException((int)response.StatusCode, "Empty response body.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var title = $"API error {(int)response.StatusCode}";

        // Try to extract the title from RFC 7807 problem details
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("title", out var t))
            {
                title = t.GetString() ?? title;
            }
        }
        catch (JsonException)
        {
            // Body is not JSON — use raw text as detail
        }

        throw new ApiException((int)response.StatusCode, title, body);
    }
}
