using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.UpdateSeoMetadata;

/// <summary>Handles <see cref="UpdateSeoMetadataCommand"/>.</summary>
public sealed class UpdateSeoMetadataCommandHandler(
  IRepository<Entry, EntryId> entryRepository)
: IRequestHandler<UpdateSeoMetadataCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
   UpdateSeoMetadataCommand request,
   CancellationToken cancellationToken)
    {
    var entryId = new EntryId(request.EntryId);
 var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
     ?? throw new NotFoundException(nameof(Entry), entryId);

     var seo = SeoMetadata.Create(request.MetaTitle, request.MetaDescription, request.CanonicalUrl, request.OgImage);
        entry.UpdateSeoMetadata(seo);
entryRepository.Update(entry);

  return Result.Success(EntryMapper.ToDto(entry));
    }
}
