namespace MicroCMS.Ai.Abstractions;

/// <summary>
/// Well-known setting keys for the AI layer.
/// All values stored via <c>SiteSettings.UpsertEntry</c> (site-specific) or
/// <c>TenantConfig.UpsertEntry</c> (tenant-wide default).
/// Read via <c>ISettingsReader</c> with site→tenant resolution chain.
/// </summary>
public static class AiSettingKeys
{
    // ── Provider ──────────────────────────────────────────────────────────

    /// <summary>Active provider name. E.g. "azure_openai", "openai", "ollama", "anthropic".</summary>
    public const string Provider = "ai:provider";

    /// <summary>Provider API endpoint URL.</summary>
    public const string Endpoint = "ai:endpoint";

    /// <summary>Provider API key. Store with <c>isSecret: true</c>.</summary>
    public const string ApiKey = "ai:api_key";

    /// <summary>Default model identifier.</summary>
    public const string Model = "ai:model";

    /// <summary>Azure OpenAI deployment name.</summary>
    public const string AzureDeployment = "ai:azure_deployment";

    /// <summary>Default maximum tokens per completion request.</summary>
    public const string MaxTokens = "ai:max_tokens";

    /// <summary>Default temperature (0.0–2.0).</summary>
    public const string Temperature = "ai:temperature";

    /// <summary>Data-residency region enforced for this tenant/site.</summary>
    public const string DataResidencyRegion = "ai:data_residency_region";

    // ── Budget ────────────────────────────────────────────────────────────

    /// <summary>Maximum tokens consumed per day. Parsed as <c>long</c>.</summary>
    public const string BudgetMaxTokensPerDay = "ai:budget:max_tokens_per_day";

    /// <summary>Monthly cost cap in USD. Parsed as <c>decimal</c>.</summary>
    public const string BudgetMonthlyCostCapUsd = "ai:budget:monthly_cost_cap_usd";

    // ── Safety ────────────────────────────────────────────────────────────

    /// <summary>Whether PII redaction is active before prompts are dispatched.</summary>
    public const string PiiRedactionEnabled = "ai:pii_redaction_enabled";

    /// <summary>Whether prompt-injection detection is active.</summary>
    public const string PromptInjectionDetectionEnabled = "ai:prompt_injection_detection_enabled";

    // ── Prompt prefix (for legacy/generic lookup) ─────────────────────────

    /// <summary>Prefix for generic feature-keyed system prompts.</summary>
    public const string SystemPromptPrefix = "ai:prompt:";

    // ── Entry content prompts ─────────────────────────────────────────────

    /// <summary>
    /// User prompt template for entry content generation.
    /// Supports placeholders: {contentType}, {fieldName}, {instructions}.
    /// </summary>
    public const string EntryContentUserPrompt = "AI.Entry.ContentUserPromt";

    /// <summary>
    /// System prompt for entry content generation.
    /// Defines the AI's role, output format constraints, and quality expectations.
    /// </summary>
    public const string EntryContentSystemPrompt = "AI.Entry.ContentSystemPrompt";

    // ── Page / SEO prompts ────────────────────────────────────────────────

    /// <summary>
    /// User prompt template for page SEO assistance.
    /// Supports placeholders: {pageTitle}, {pageContent}, {targetKeywords}.
    /// </summary>
    public const string PageSeoUserPrompt = "AI.Page.SEOUserPrompt";

    /// <summary>
    /// System prompt for page SEO assistance.
    /// Defines tone, output JSON schema, and SEO guidelines.
    /// </summary>
    public const string PageSeoSystemPrompt = "AI.Page.SEOSystemPrompt";

    // ── Vector / RAG ─────────────────────────────────────────────────────

    /// <summary>Vector store provider. E.g. "pgvector", "qdrant".</summary>
    public const string VectorStoreProvider = "ai:vector_store:provider";

    /// <summary>Vector store endpoint URL.</summary>
    public const string VectorStoreEndpoint = "ai:vector_store:endpoint";

    /// <summary>Vector store API key. Store with <c>isSecret: true</c>.</summary>
    public const string VectorStoreApiKey = "ai:vector_store:api_key";

    /// <summary>Embedding model identifier.</summary>
    public const string EmbeddingModel = "ai:embedding_model";

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// All AI setting keys that hold secrets and must be redacted in read responses.
    /// </summary>
    public static readonly IReadOnlySet<string> SecretKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ApiKey,
        VectorStoreApiKey,
    };

    /// <summary>
    /// All AI prompt setting keys. Used by export/import wizard to identify prompt entries.
    /// </summary>
    public static readonly IReadOnlySet<string> PromptKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        EntryContentUserPrompt,
        EntryContentSystemPrompt,
        PageSeoUserPrompt,
        PageSeoSystemPrompt,
    };

    /// <summary>Category label for all AI settings entries.</summary>
    public const string Category = "ai";

    /// <summary>Category label for AI prompt entries.</summary>
    public const string PromptCategory = "ai:prompts";
}
