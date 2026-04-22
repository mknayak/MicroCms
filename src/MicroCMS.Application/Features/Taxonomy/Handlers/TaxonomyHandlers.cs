using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Taxonomy.Commands;
using MicroCMS.Application.Features.Taxonomy.Dtos;
using MicroCMS.Application.Features.Taxonomy.Queries;
using MicroCMS.Domain.Aggregates.Taxonomy;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Taxonomy;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Taxonomy.Handlers;

internal static class TaxonomyMapper
{
    internal static CategoryDto ToDto(Category c) => new(
  c.Id.Value, c.SiteId.Value, c.Name, c.Slug.Value,
  c.ParentId?.Value, c.Description, c.CreatedAt, c.UpdatedAt);

    internal static TagDto ToDto(Tag t) => new(
        t.Id.Value, t.SiteId.Value, t.Name, t.Slug.Value, t.CreatedAt);
}

internal sealed class CreateCategoryCommandHandler(
    IRepository<Category, CategoryId> repo,
    ICurrentUser currentUser)
    : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
     var parentId = request.ParentId.HasValue ? new CategoryId(request.ParentId.Value) : (CategoryId?)null;
        var category = Category.Create(
   currentUser.TenantId, new SiteId(request.SiteId),
   request.Name, Slug.Create(request.Slug), parentId, request.Description);

 await repo.AddAsync(category, cancellationToken);
        return Result.Success(TaxonomyMapper.ToDto(category));
    }
}

internal sealed class DeleteCategoryCommandHandler(
    IRepository<Category, CategoryId> repo)
    : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
     var category = await repo.GetByIdAsync(new CategoryId(request.CategoryId), cancellationToken)
    ?? throw new NotFoundException(nameof(Category), request.CategoryId);
       repo.Remove(category);
     return Result.Success();
    }
}

internal sealed class CreateTagCommandHandler(
    IRepository<Tag, TagId> repo,
    ICurrentUser currentUser)
    : IRequestHandler<CreateTagCommand, Result<TagDto>>
{
    public async Task<Result<TagDto>> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
var tag = Tag.Create(
       currentUser.TenantId, new SiteId(request.SiteId),
  request.Name, Slug.Create(request.Slug));
        await repo.AddAsync(tag, cancellationToken);
   return Result.Success(TaxonomyMapper.ToDto(tag));
    }
}

internal sealed class DeleteTagCommandHandler(
    IRepository<Tag, TagId> repo)
    : IRequestHandler<DeleteTagCommand, Result>
{
    public async Task<Result> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await repo.GetByIdAsync(new TagId(request.TagId), cancellationToken)
   ?? throw new NotFoundException(nameof(Tag), request.TagId);
      repo.Remove(tag);
    return Result.Success();
    }
}

internal sealed class ListCategoriesQueryHandler(
    IRepository<Category, CategoryId> repo)
    : IRequestHandler<ListCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var items = await repo.ListAsync(new CategoriesBySiteSpec(new SiteId(request.SiteId)), cancellationToken);
     return Result.Success<IReadOnlyList<CategoryDto>>(items.Select(TaxonomyMapper.ToDto).ToList().AsReadOnly());
    }
}

internal sealed class ListTagsQueryHandler(
    IRepository<Tag, TagId> repo)
    : IRequestHandler<ListTagsQuery, Result<IReadOnlyList<TagDto>>>
{
    public async Task<Result<IReadOnlyList<TagDto>>> Handle(ListTagsQuery request, CancellationToken cancellationToken)
    {
     var items = await repo.ListAsync(new TagsBySiteSpec(new SiteId(request.SiteId)), cancellationToken);
        return Result.Success<IReadOnlyList<TagDto>>(items.Select(TaxonomyMapper.ToDto).ToList().AsReadOnly());
    }
}
