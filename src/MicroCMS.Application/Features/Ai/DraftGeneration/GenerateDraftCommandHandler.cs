using System.Text.Json;
using Json.Schema;
using MediatR;
using Microsoft.Extensions.Logging;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Settings;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Ai.DraftGeneration;

public sealed class GenerateDraftCommandHandler : IRequestHandler<GenerateDraftCommand, Result<GeneratedDraftDto>>
{
    private readonly IRepository<ContentType, ContentTypeId> _contentTypeRepository;
    private readonly ILlmService _llmService;
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<GenerateDraftCommandHandler> _logger;

    public GenerateDraftCommandHandler(
        IRepository<ContentType, ContentTypeId> contentTypeRepository,
        ILlmService llmService,
        ISettingsReader settingsReader,
        ILogger<GenerateDraftCommandHandler> logger)
    {
        _contentTypeRepository = contentTypeRepository;
        _llmService = llmService;
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public async Task<Result<GeneratedDraftDto>> Handle(
        GenerateDraftCommand request,
        CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var contentTypeId = new ContentTypeId(request.ContentTypeId);
        var contentType = await _contentTypeRepository.GetByIdAsync(contentTypeId, cancellationToken);

        if (contentType is null)
            return Result.Failure<GeneratedDraftDto>(
                Error.NotFound("ContentType.NotFound", $"Content type '{request.ContentTypeId}' not found."));

        var schema = BuildSchema(contentType);
        var schemaJson = JsonSerializer.Serialize(schema);

        var systemPrompt = await _settingsReader.GetAsync(siteId, AiSettingKeys.EntryContentSystemPrompt, cancellationToken);
        var userPromptTemplate = await _settingsReader.GetAsync(siteId, AiSettingKeys.EntryContentUserPrompt, cancellationToken);

        if (string.IsNullOrWhiteSpace(systemPrompt) || string.IsNullOrWhiteSpace(userPromptTemplate))
            return Result.Failure<GeneratedDraftDto>(
                Error.Validation("Ai.PromptsNotConfigured",
                    "AI prompts are not configured for this site. Import ai.settings.json via Site Settings."));

        var fullSystemPrompt = systemPrompt + "\n\nGenerate content matching this JSON schema:\n" + schemaJson;
        var userMessage = userPromptTemplate
            .Replace("{contentType}", contentType.DisplayName, StringComparison.Ordinal)
            .Replace("{instructions}", request.Prompt, StringComparison.Ordinal);

        var llmRequest = new LlmRequest(
            SystemPrompt: fullSystemPrompt,
            UserMessage: userMessage,
            FeatureHint: "draft_generation",
            ResponseFormat: "json_object");

        var response = await _llmService.CompleteAsync(llmRequest, cancellationToken);

        Dictionary<string, object> fields;
        try
        {
            fields = JsonSerializer.Deserialize<Dictionary<string, object>>(response.Content)
                     ?? throw new InvalidOperationException("Response JSON was null.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "AI draft response was not valid JSON for ContentType {Id}", contentTypeId);
            return Result.Failure<GeneratedDraftDto>(
                Error.Validation("Ai.InvalidResponse",
                    "The AI returned a response that could not be parsed. Please try again."));
        }

        var title = fields.TryGetValue("title", out var titleObj)
            ? titleObj?.ToString() ?? "Untitled"
            : "Untitled";

        _logger.LogInformation(
            "Generated draft for ContentType {ContentTypeId} via {Provider}/{Model}, tokens: {Tokens}",
            contentTypeId, response.ProviderName, response.Model,
            response.PromptTokens + response.CompletionTokens);

        return Result.Success<GeneratedDraftDto>(new GeneratedDraftDto
        {
            Fields = fields,
            Title = title,
            TokensUsed = response.PromptTokens + response.CompletionTokens
        });
    }

    private static JsonSchema BuildSchema(ContentType contentType)
    {
        var properties = new Dictionary<string, JsonSchema>();

        foreach (var field in contentType.Fields)
        {
            JsonSchema fieldSchema;

            if (field.FieldType == Domain.Enums.FieldType.ShortText)
                fieldSchema = new JsonSchemaBuilder().Type(SchemaValueType.String).MaxLength(255).Build();
            else if (field.FieldType == Domain.Enums.FieldType.Integer)
                fieldSchema = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build();
            else if (field.FieldType == Domain.Enums.FieldType.Decimal)
                fieldSchema = new JsonSchemaBuilder().Type(SchemaValueType.Number).Build();
            else if (field.FieldType == Domain.Enums.FieldType.Boolean)
                fieldSchema = new JsonSchemaBuilder().Type(SchemaValueType.Boolean).Build();
            else
                fieldSchema = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();

            properties[field.Handle] = fieldSchema;
        }

        var builder = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Title("Schema for " + contentType.DisplayName)
            .Properties(properties);

        var required = contentType.Fields.Where(f => f.IsRequired).Select(f => f.Handle).ToList();
        if (required.Count > 0)
            builder = builder.Required(required);

        return builder.Build();
    }
}
