using FluentValidation;

namespace MicroCMS.Application.Features.Folders.Commands;

public sealed class CreateFolderCommandValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderCommandValidator()
    {
   RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class RenameFolderCommandValidator : AbstractValidator<RenameFolderCommand>
{
    public RenameFolderCommandValidator()
    {
  RuleFor(x => x.NewName).NotEmpty().MaximumLength(200);
    }
}
