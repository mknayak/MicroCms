using FluentValidation;

namespace MicroCMS.Application.Features.Entries.Commands.UpdateEntry;

/// <summary>Validates <see cref="UpdateEntryCommand"/> before dispatch.</summary>
public sealed class UpdateEntryCommandValidator : AbstractValidator<UpdateEntryCommand>
{
    public UpdateEntryCommandValidator()
    {
        RuleFor(c => c.EntryId)
            .NotEmpty().WithMessage("EntryId is required.");

        RuleFor(c => c.FieldsJson)
            .NotEmpty().WithMessage("FieldsJson must not be empty.")
            .Must(BeValidJson).WithMessage("FieldsJson must be valid JSON.");

        When(c => c.NewSlug is not null, () =>
        {
            RuleFor(c => c.NewSlug!)
                .MaximumLength(200).WithMessage("Slug must not exceed 200 characters.")
                .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Slug may only contain lowercase letters, digits, and hyphens.");
        });

        When(c => c.ChangeNote is not null, () =>
        {
            RuleFor(c => c.ChangeNote!)
                .MaximumLength(500).WithMessage("ChangeNote must not exceed 500 characters.");
        });
    }

    private static bool BeValidJson(string json)
    {
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
