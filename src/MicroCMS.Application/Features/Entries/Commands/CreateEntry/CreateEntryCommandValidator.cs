using FluentValidation;

namespace MicroCMS.Application.Features.Entries.Commands.CreateEntry;

/// <summary>
/// Validates <see cref="CreateEntryCommand"/> before the handler is invoked.
/// Business-rule slug uniqueness is enforced by the handler; structural checks belong here.
/// </summary>
public sealed class CreateEntryCommandValidator : AbstractValidator<CreateEntryCommand>
{
    public CreateEntryCommandValidator()
    {
        RuleFor(c => c.SiteId)
            .NotEmpty().WithMessage("SiteId is required.");

        RuleFor(c => c.ContentTypeId)
            .NotEmpty().WithMessage("ContentTypeId is required.");

        RuleFor(c => c.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug may only contain lowercase letters, digits, and hyphens, and must not start or end with a hyphen.");

        RuleFor(c => c.Locale)
            .NotEmpty().WithMessage("Locale is required.")
            .MaximumLength(35).WithMessage("Locale must not exceed 35 characters.")
            .Matches(@"^[a-zA-Z]{2,8}(-[a-zA-Z0-9]{1,8}){0,3}$")
            .WithMessage("Locale must be a valid BCP 47 code (e.g. 'en', 'en-US').");

        RuleFor(c => c.FieldsJson)
            .NotEmpty().WithMessage("FieldsJson must not be empty.")
            .Must(BeValidJson).WithMessage("FieldsJson must be valid JSON.");
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
