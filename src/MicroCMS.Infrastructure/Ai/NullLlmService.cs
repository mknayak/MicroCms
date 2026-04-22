using MicroCMS.Application.Common.Interfaces;

namespace MicroCMS.Infrastructure.Ai;

/// <summary>
/// No-op LLM service used when no AI provider is configured.
/// Returns an empty response so the application starts successfully;
/// replace with a real provider adapter (e.g. LlmServiceRouter in Ai.Core)
/// once AI credentials are configured.
/// </summary>
internal sealed class NullLlmService : ILlmService
{
    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
 => Task.FromResult(new LlmResponse(
     Content: string.Empty,
            PromptTokens: 0,
        CompletionTokens: 0,
      ProviderName: "null",
  Model: "none"));

  public async IAsyncEnumerable<string> StreamAsync(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
        await Task.CompletedTask;
    yield break;
    }
}
