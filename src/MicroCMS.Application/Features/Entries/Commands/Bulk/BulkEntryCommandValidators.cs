using FluentValidation;

namespace MicroCMS.Application.Features.Entries.Commands.Bulk;

public sealed class BulkPublishEntriesCommandValidator : AbstractValidator<BulkPublishEntriesCommand>
{
    public BulkPublishEntriesCommandValidator()
    {
        RuleFor(x => x.EntryIds).NotEmpty().WithMessage("At least one entry ID is required.");
        RuleFor(x => x.EntryIds.Count).LessThanOrEqualTo(100).WithMessage("Maximum 100 entries per bulk operation.");
    }
}

public sealed class BulkUnpublishEntriesCommandValidator : AbstractValidator<BulkUnpublishEntriesCommand>
{
    public BulkUnpublishEntriesCommandValidator()
    {
        RuleFor(x => x.EntryIds).NotEmpty().WithMessage("At least one entry ID is required.");
        RuleFor(x => x.EntryIds.Count).LessThanOrEqualTo(100).WithMessage("Maximum 100 entries per bulk operation.");
    }
}

public sealed class BulkDeleteEntriesCommandValidator : AbstractValidator<BulkDeleteEntriesCommand>
{
public BulkDeleteEntriesCommandValidator()
    {
     RuleFor(x => x.EntryIds).NotEmpty().WithMessage("At least one entry ID is required.");
        RuleFor(x => x.EntryIds.Count).LessThanOrEqualTo(100).WithMessage("Maximum 100 entries per bulk operation.");
    }
}
