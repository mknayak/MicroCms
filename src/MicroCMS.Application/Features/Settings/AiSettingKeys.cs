namespace MicroCMS.Application.Features.Settings;

/// <summary>
/// Well-known setting keys for the AI enabler layer (GAP-AI-1).
///
/// All keys follow <c>ai:{sub-category}:{name}</c>.
/// Store values via <c>TenantConfig.UpsertEntry</c> (tenant-wide) or
/// <c>SiteSettings.UpsertEntry</c> (per-site override).
/// Read values via <see cref="MicroCMS.Application.Common.Interfaces.ISettingsReader"/>.
/// </summary>
public static class AiSettingKeys
{
    // ── Provider ──────────────────────────────────────────────────────────

    /// <summary>Active provider name. E.g. "azure_openai", "openai", "ollama", "anthropic".</summary>
    public const string Provider = "ai:provider";

    /// <summary>Provider API endpoint URL.</summary>
    public const string Endpoint = "ai:endpoint";

    /// <summary>Provider API key. Store with <c>isSecret: true</c> — redacted in read APIs.</summary>
    public const string ApiKey = "ai:api_key";

    /// <summary>Default model identifier. E.g. "gpt-4o", "claude-3-5-sonnet", "llama3".</summary>
    public const string Model = "ai:model";

    /// <summary>Azure OpenAI deployment name (overrides <see cref="Model"/> for Azure).</summary>
    public const string AzureDeployment = "ai:azure_deployment";

    /// <summary>Default maximum tokens per completion request. Parsed as <c>int</c>.</summary>
    public const string MaxTokens = "ai:max_tokens";

    /// <summary>Default temperature (0.0–2.0). Parsed as <c>float</c>.</summary>
    public const string Temperature = "ai:temperature";

    /// <summary>Data-residency region enforced for this tenant. E.g. "eastus", "westeurope".</summary>
    public const string DataResidencyRegion = "ai:data_residency_region";

    // ── System prompts ────────────────────────────────────────────────────

    /// <summary>System prompt for the draft-content feature.</summary>
    public const string SystemPromptDraft = "ai:system_prompt:draft";

    /// <summary>System prompt for the rewrite-content feature.</summary>
    public const string SystemPromptRewrite = "ai:system_prompt:rewrite";

    /// <summary>System prompt for the summarise feature.</summary>
    public const string SystemPromptSummarise = "ai:system_prompt:summarise";

    /// <summary>System prompt for the SEO-assistance feature.</summary>
    public const string SystemPromptSeo = "ai:system_prompt:seo";

    /// <summary>System prompt for the translation feature.</summary>
    public const string SystemPromptTranslation = "ai:system_prompt:translation";

    /// <summary>
    /// Format string for tone-specific system prompts.
    /// Use <see cref="TonePromptKey"/> rather than formatting this directly.
    /// </summary>
    public const string SystemPromptToneFormat = "ai:system_prompt:tone:{0}";

    /// <summary>Returns the setting key for the system prompt of <paramref name="tone"/>.</summary>
    public static string TonePromptKey(string tone) =>
        string.Format(SystemPromptToneFormat, tone.ToLowerInvariant());

    // ── Budget ────────────────────────────────────────────────────────────

    /// <summary>Maximum tokens consumed per tenant per day. Parsed as <c>long</c>.</summary>
    public const string BudgetMaxTokensPerDay = "ai:budget:max_tokens_per_day";

    /// <summary>Monthly cost cap in USD. Parsed as <c>decimal</c>.</summary>
    public const string BudgetMonthlyCostCapUsd = "ai:budget:monthly_cost_cap_usd";

    // ── Safety ────────────────────────────────────────────────────────────

    /// <summary>Whether PII redaction is active before prompts are dispatched. Default "true".</summary>
    public const string PiiRedactionEnabled = "ai:pii_redaction_enabled";

    /// <summary>Whether prompt-injection detection is active. Default "true".</summary>
    public const string PromptInjectionDetectionEnabled = "ai:prompt_injection_detection_enabled";

    /// <summary>Whether the post-call safety classifier is active. Default "true".</summary>
    public const string SafetyPostFilterEnabled = "ai:safety_post_filter_enabled";

    // ── Vector / RAG ─────────────────────────────────────────────────────

    /// <summary>Vector store provider. E.g. "pgvector", "qdrant".</summary>
    public const string VectorStoreProvider = "ai:vector_store:provider";

    /// <summary>Vector store endpoint URL.</summary>
    public const string VectorStoreEndpoint = "ai:vector_store:endpoint";

    /// <summary>Vector store API key. Store with <c>isSecret: true</c>.</summary>
    public const string VectorStoreApiKey = "ai:vector_store:api_key";

    /// <summary>Embedding model used to index and query content.</summary>
    public const string EmbeddingModel = "ai:embedding_model";
}
