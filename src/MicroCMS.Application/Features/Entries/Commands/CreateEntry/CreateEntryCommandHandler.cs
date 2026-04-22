using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.CreateEntry;

/// <summary>
/// Handles <see cref="CreateEntryCommand"/>:
/// 1. Validates slug uniqueness within the site+locale scope.
/// 2. Constructs the Entry aggregate via its factory method.
/// 3. Persists via the repository (SaveChanges is deferred to UnitOfWorkBehavior).
/// </summary>
public sealed class CreateEntryCommandHandler(
    IRepository<Entry, EntryId> entryRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CreateEntryCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
        CreateEntryCommand request,
        CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var contentTypeId = new ContentTypeId(request.ContentTypeId);
        var slug = Slug.Create(request.Slug);
        var locale = Locale.Create(request.Locale);

        var slugConflict = await CheckSlugConflictAsync(siteId, slug.Value, locale.Value, entryId: null, cancellationToken);
        if (slugConflict is not null)
        {
            return Result.Failure<EntryDto>(slugConflict);
        }

        var entry = Entry.Create(
            tenantId: currentUser.TenantId,
            siteId: siteId,
            contentTypeId: contentTypeId,
            slug: slug,
            locale: locale,
            authorId: currentUser.UserId,
            fieldsJson: request.FieldsJson);

        await entryRepository.AddAsync(entry, cancellationToken);

        return Result.Success(EntryMapper.ToDto(entry));
    }

    private async Task<Error?> CheckSlugConflictAsync(
        SiteId siteId,
        string slug,
        string locale,
        EntryId? entryId,
        CancellationToken cancellationToken)
    {
        var spec = new EntryBySlugAndSiteSpec(siteId, slug, locale);
        var existing = await entryRepository.ListAsync(spec, cancellationToken);

        var conflict = existing.FirstOrDefault(e => entryId == null || e.Id != entryId);
        if (conflict is null)
        {
            return null;
        }

        return Error.Conflict(
            "Entry.SlugConflict",
            $"A '{locale}' entry with slug '{slug}' already exists in this site.");
    }
}
