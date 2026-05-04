using System.Text.Json;
using Json.Schema;
using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions.Dtos;

namespace MicroCMS.Ai.Core.Services;

/// <summary>
/// Validates AI-generated JSON responses against JSON Schema and provides repair loops.
/// Max 2 repair attempts before failing.
/// </summary>
public sealed class StructuredOutputValidator
{
    private readonly ILogger<StructuredOutputValidator> _logger;
    private const int MaxRepairAttempts = 2;

    public StructuredOutputValidator(ILogger<StructuredOutputValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates JSON response against the provided schema.
    /// Returns validation result with detailed error messages if validation fails.
    /// </summary>
    public ValidationResult Validate(string jsonResponse, JsonSchema schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonResponse, nameof(jsonResponse));
        ArgumentNullException.ThrowIfNull(schema, nameof(schema));

        try
        {
            // Parse JSON
            using var doc = JsonDocument.Parse(jsonResponse);
            var jsonElement = doc.RootElement;

            // Validate against schema
            var evaluationResults = schema.Evaluate(jsonElement);

            if (evaluationResults.IsValid)
            {
                _logger.LogDebug("JSON response validation succeeded");
                return ValidationResult.Success();
            }

            // Collect error messages
            var errors = new List<string>();
            CollectErrors(evaluationResults, errors);

            _logger.LogWarning(
                "JSON response validation failed with {Count} errors: {Errors}",
                errors.Count,
                string.Join("; ", errors));

            return ValidationResult.Failure(errors);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response during validation");
            return ValidationResult.Failure(new[] { $"Invalid JSON: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during JSON validation");
            return ValidationResult.Failure(new[] { $"Validation error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Validates and attempts to repair the response up to MaxRepairAttempts times.
    /// Returns the valid JSON or throws if all repair attempts fail.
    /// </summary>
    public async Task<string> ValidateAndRepairAsync(
        string jsonResponse,
        JsonSchema schema,
        Func<string, Task<string>> repairFunction,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonResponse, nameof(jsonResponse));
        ArgumentNullException.ThrowIfNull(schema, nameof(schema));
        ArgumentNullException.ThrowIfNull(repairFunction, nameof(repairFunction));

        var currentResponse = jsonResponse;
        var attempt = 0;

        while (attempt <= MaxRepairAttempts)
        {
            var validationResult = Validate(currentResponse, schema);

            if (validationResult.IsValid)
            {
                if (attempt > 0)
                {
                    _logger.LogInformation(
                        "JSON response repaired successfully after {Attempts} attempt(s)",
                        attempt);
                }
                return currentResponse;
            }

            if (attempt == MaxRepairAttempts)
            {
                _logger.LogError(
                    "JSON response validation failed after {MaxAttempts} repair attempts. Errors: {Errors}",
                    MaxRepairAttempts,
                    string.Join("; ", validationResult.Errors));

                throw new InvalidOperationException(
                    $"AI response validation failed after {MaxRepairAttempts} repair attempts. " +
                    $"Errors: {string.Join("; ", validationResult.Errors)}");
            }

            // Attempt repair
            attempt++;
            _logger.LogInformation(
                "Attempting to repair JSON response (attempt {Attempt}/{MaxAttempts}). Errors: {Errors}",
                attempt,
                MaxRepairAttempts,
                string.Join("; ", validationResult.Errors));

            try
            {
                currentResponse = await repairFunction(currentResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Repair function failed on attempt {Attempt}", attempt);
                throw new InvalidOperationException(
                    $"Failed to repair AI response on attempt {attempt}: {ex.Message}",
                    ex);
            }
        }

        // Should never reach here due to loop logic, but included for completeness
        throw new InvalidOperationException("Unexpected validation state");
    }

    /// <summary>
    /// Attempts to extract and validate JSON from a response that may contain extra text.
    /// Useful when AI providers return JSON wrapped in markdown code blocks or prose.
    /// </summary>
    public ValidationResult TryExtractAndValidate(string rawResponse, JsonSchema schema, out string? extractedJson)
    {
        extractedJson = null;

        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return ValidationResult.Failure(new[] { "Response is empty" });
        }

        // Try to extract JSON from markdown code blocks
        var jsonCandidates = new List<string>();

        // Pattern 1: ```json ... ```
        var jsonBlockMatch = System.Text.RegularExpressions.Regex.Match(
            rawResponse,
            @"```json\s*(.*?)\s*```",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (jsonBlockMatch.Success)
        {
            jsonCandidates.Add(jsonBlockMatch.Groups[1].Value.Trim());
        }

        // Pattern 2: ``` ... ``` (generic code block)
        var codeBlockMatch = System.Text.RegularExpressions.Regex.Match(
            rawResponse,
            @"```\s*(.*?)\s*```",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (codeBlockMatch.Success)
        {
            jsonCandidates.Add(codeBlockMatch.Groups[1].Value.Trim());
        }

        // Pattern 3: First { ... } or [ ... ]
        var jsonObjectMatch = System.Text.RegularExpressions.Regex.Match(
            rawResponse,
            @"(\{.*\}|\[.*\])",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (jsonObjectMatch.Success)
        {
            jsonCandidates.Add(jsonObjectMatch.Groups[1].Value.Trim());
        }

        // Try raw response as-is
        jsonCandidates.Add(rawResponse.Trim());

        // Validate each candidate
        foreach (var candidate in jsonCandidates.Distinct())
        {
            var result = Validate(candidate, schema);
            if (result.IsValid)
            {
                extractedJson = candidate;
                _logger.LogDebug("Successfully extracted and validated JSON from raw response");
                return result;
            }
        }

        _logger.LogWarning("Failed to extract valid JSON from raw response");
        return ValidationResult.Failure(new[] { "No valid JSON found in response" });
    }

    private static void CollectErrors(EvaluationResults results, List<string> errors, string path = "$")
    {
        if (!results.IsValid)
        {
            // Json.Schema doesn't expose a simple error message property
            // We'll just note the path where validation failed
            errors.Add($"{path}: Validation failed");
        }

        if (results.Details != null)
        {
            foreach (var detail in results.Details)
            {
                var newPath = string.IsNullOrEmpty(detail.EvaluationPath.ToString())
                    ? path
                    : $"{path}.{detail.EvaluationPath}";

                CollectErrors(detail, errors, newPath);
            }
        }
    }

    public sealed class ValidationResult
    {
        public bool IsValid { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Failure(IEnumerable<string> errors) => new()
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }
}
