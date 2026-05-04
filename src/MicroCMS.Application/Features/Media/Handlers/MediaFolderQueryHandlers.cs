using MediatR;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Application.Features.Media.Mappers;
using MicroCMS.Application.Features.Media.Queries;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Media;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Media.Handlers;

internal sealed class ListMediaFoldersQueryHandler(
    IRepository<MediaFolder, Guid> repo,
    ICurrentUser currentUser) : IRequestHandler<ListMediaFoldersQuery, Result<IReadOnlyList<MediaFolderDto>>>
{
    public async Task<Result<IReadOnlyList<MediaFolderDto>>> Handle(
        ListMediaFoldersQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<IReadOnlyList<MediaFolderDto>>(Error.Validation("Auth.NoSiteContext", "No site context in token. Call POST /auth/switch-site first."));

        var folders = await repo.ListAsync(
            new MediaFoldersBySiteSpec(siteId, request.ParentFolderId), cancellationToken);

        return Result.Success<IReadOnlyList<MediaFolderDto>>(
            folders.Select(MediaMapper.ToFolderDto).ToList().AsReadOnly());
    }
}

internal sealed class GetMediaFolderQueryHandler(
    IRepository<MediaFolder, Guid> repo) : IRequestHandler<GetMediaFolderQuery, Result<MediaFolderDto>>
{
    public async Task<Result<MediaFolderDto>> Handle(
        GetMediaFolderQuery request,
        CancellationToken cancellationToken)
    {
        var folder = await repo.GetByIdAsync(request.FolderId, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaFolder), request.FolderId);

        return Result.Success(MediaMapper.ToFolderDto(folder));
    }
}
