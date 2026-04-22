using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.UpdateEntry;

/// <summary>
/// Handles <see cref="UpdateEntryCommand"/>:
/// 1. Loads the entry (throws NotFoundException if missing).
/// 2. Validates optional slug change for uniqueness.
/// 3. Delegates mutation to the Entry aggregate.
/// </summary>
public sealed class UpdateEntryCommandHandler(
    IRepository<Entry, EntryId> entryRepository,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateEntryCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
        UpdateEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entryId = new EntryId(request.EntryId);
        var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), entryId);

        if (request.NewSlug is not null)
        {
            var newSlug = Slug.Create(request.NewSlug);
            var conflict = await CheckSlugConflictAsync(entry.SiteId, newSlug.Value, entry.Locale.Value, entryId, cancellationToken);
            if (conflict is not null)
            {
                return Result.Failure<EntryDto>(conflict);
            }

            entry.UpdateSlug(newSlug);
        }

        entry.UpdateFields(request.FieldsJson, currentUser.UserId, request.ChangeNote);
        entryRepository.Update(entry);

        return Result.Success(EntryMapper.ToDto(entry));
    }

    private async Task<Error?> CheckSlugConflictAsync(
        SiteId siteId,
        string slug,
        string locale,
        EntryId excludeId,
        CancellationToken cancellationToken)
    {
        var spec = new EntryBySlugAndSiteSpec(siteId, slug, locale);
        var existing = await entryRepository.ListAsync(spec, cancellationToken);

        var conflict = existing.FirstOrDefault(e => e.Id != excludeId);
        if (conflict is null)
        {
            return null;
        }

        return Error.Conflict(
            "Entry.SlugConflict",
            $"A '{locale}' entry with slug '{slug}' already exists in this site.");
    }
}
