using FluentValidation;

namespace MicroCMS.Application.Features.Entries.Commands.SchedulePublish;

/// <summary>Validates <see cref="SchedulePublishCommand"/> before dispatch.</summary>
public sealed class SchedulePublishCommandValidator : AbstractValidator<SchedulePublishCommand>
{
    public SchedulePublishCommandValidator()
    {
        RuleFor(c => c.EntryId)
            .NotEmpty().WithMessage("EntryId is required.");

        RuleFor(c => c.PublishAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("PublishAt must be a future date/time.");

        When(c => c.UnpublishAt.HasValue, () =>
        {
            RuleFor(c => c.UnpublishAt!.Value)
                .GreaterThan(c => c.PublishAt)
                .WithMessage("UnpublishAt must be after PublishAt.");
        });
    }
}
