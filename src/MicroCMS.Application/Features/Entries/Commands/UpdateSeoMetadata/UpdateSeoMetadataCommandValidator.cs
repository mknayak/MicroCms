using FluentValidation;
using MicroCMS.Domain.ValueObjects;

namespace MicroCMS.Application.Features.Entries.Commands.UpdateSeoMetadata;

public sealed class UpdateSeoMetadataCommandValidator : AbstractValidator<UpdateSeoMetadataCommand>
{
 public UpdateSeoMetadataCommandValidator()
    {
    RuleFor(x => x.MetaTitle)
        .MaximumLength(SeoMetadata.MaxMetaTitleLength)
      .When(x => x.MetaTitle is not null);

RuleFor(x => x.MetaDescription)
           .MaximumLength(SeoMetadata.MaxMetaDescriptionLength)
   .When(x => x.MetaDescription is not null);

     RuleFor(x => x.CanonicalUrl)
 .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
    .WithMessage("CanonicalUrl must be a valid absolute URL.")
            .When(x => !string.IsNullOrEmpty(x.CanonicalUrl));
    }
}
