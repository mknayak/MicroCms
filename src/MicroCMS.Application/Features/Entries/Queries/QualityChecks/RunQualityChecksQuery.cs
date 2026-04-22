using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;
using System.Text.RegularExpressions;

namespace MicroCMS.Application.Features.Entries.Queries.QualityChecks;

/// <summary>Runs grammar, readability, and PII checks on an entry's content (GAP-09).</summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record RunQualityChecksQuery(Guid EntryId) : IQuery<QualityCheckReport>;

/// <summary>Aggregated quality check results returned to the UI.</summary>
public sealed record QualityCheckReport(
    Guid EntryId,
    double GrammarScore,
    string ReadabilityGrade,
    bool PiiDetected,
    IReadOnlyList<PiiMatch> PiiMatches,
    IReadOnlyList<string> Suggestions);

public sealed record PiiMatch(string Type, string RedactedValue, int Position);

/// <summary>Handles <see cref="RunQualityChecksQuery"/>.</summary>
public sealed class RunQualityChecksQueryHandler(
    IRepository<Entry, EntryId> entryRepository,
  ILlmService llm)
    : IRequestHandler<RunQualityChecksQuery, Result<QualityCheckReport>>
{
    // PII patterns — regex-based, no network call required (GAP-09, GAP-27 safety)
    private static readonly (string Type, Regex Pattern)[] PiiPatterns =
  [
    ("EMAIL",     new Regex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)),
        ("PHONE",       new Regex(@"\b(\+?[\d\s\-().]{7,15})\b", RegexOptions.Compiled)),
        ("SSN", new Regex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)),
  ("CREDIT_CARD", new Regex(@"\b(?:\d[ \-]?){13,16}\b", RegexOptions.Compiled)),
        ("IP_ADDRESS",  new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled)),
    ];

    public async Task<Result<QualityCheckReport>> Handle(
        RunQualityChecksQuery request,
        CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), request.EntryId);

        var piiMatches = DetectPii(entry.FieldsJson);

        var llmRequest = new LlmRequest(
  SystemPrompt: "You are a content quality analyser. Respond with JSON only: " +
     "{\"grammarScore\": 0.0-1.0, \"readabilityGrade\": \"A-F\", \"suggestions\": [\"...\"] }",
            UserMessage: entry.FieldsJson,
  FeatureHint: "quality_check",
 Temperature: 0.2f,
            ResponseFormat: "json_object");

        var response = await llm.CompleteAsync(llmRequest, cancellationToken);
    var (grammarScore, readabilityGrade, suggestions) = ParseLlmReport(response.Content);

        var report = new QualityCheckReport(
            entry.Id.Value,
            grammarScore,
         readabilityGrade,
piiMatches.Count > 0,
            piiMatches,
            suggestions);

        return Result.Success(report);
    }

    private static IReadOnlyList<PiiMatch> DetectPii(string text)
    {
        var matches = new List<PiiMatch>();
     foreach (var (type, pattern) in PiiPatterns)
            foreach (Match m in pattern.Matches(text))
      matches.Add(new PiiMatch(type, new string('*', m.Length), m.Index));
        return matches.AsReadOnly();
    }

    private static (double GrammarScore, string ReadabilityGrade, IReadOnlyList<string> Suggestions)
    ParseLlmReport(string json)
    {
        try
        {
      using var doc = System.Text.Json.JsonDocument.Parse(json);
      var root = doc.RootElement;
        var score = root.TryGetProperty("grammarScore", out var gs) ? gs.GetDouble() : 0.8;
    var grade = root.TryGetProperty("readabilityGrade", out var rg) ? rg.GetString() ?? "B" : "B";
       var suggestions = root.TryGetProperty("suggestions", out var sg)
    ? sg.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList().AsReadOnly()
                : (IReadOnlyList<string>)[];
     return (score, grade, suggestions);
        }
   catch
        {
       return (0.8, "B", []);
        }
    }
}
