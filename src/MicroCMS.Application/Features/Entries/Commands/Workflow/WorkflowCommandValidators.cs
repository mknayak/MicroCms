using FluentValidation;

namespace MicroCMS.Application.Features.Entries.Commands.Workflow;

public sealed class RejectEntryCommandValidator : AbstractValidator<RejectEntryCommand>
{
 public RejectEntryCommandValidator()
    {
  RuleFor(x => x.Reason)
        .NotEmpty()
   .WithMessage("A rejection reason is required.")
  .MaximumLength(500);
    }
}
