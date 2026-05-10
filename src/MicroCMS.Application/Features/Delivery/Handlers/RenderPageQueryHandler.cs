using MediatR;
using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Application.Features.Delivery.Pipeline;
using MicroCMS.Application.Features.Delivery.Queries;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Delivery.Handlers;

/// <summary>
/// Thin MediatR handler that delegates page rendering to <see cref="PageRenderPipeline"/>.
///
/// The pipeline is a Chain-of-Responsibility chain of <see cref="IPageRenderStep"/> steps
/// registered in DI. Each step enriches a shared <see cref="PageRenderContext"/> and passes
/// control to the next. The handler's only responsibility is building the initial context
/// and returning the pipeline result.
///
/// To add a new rendering concern (e.g. A/B testing, personalisation, pre-render export),
/// implement <see cref="IPageRenderStep"/>, register it in DI at the desired position,
/// and no existing step needs to change.
/// </summary>
internal sealed class RenderPageBySlugQueryHandler(PageRenderPipeline pipeline)
    : IRequestHandler<RenderPageBySlugQuery, Result<RenderedPageDto>>
{
    public async Task<Result<RenderedPageDto>> Handle(
        RenderPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var ctx = new PageRenderContext
        {
            SiteId = new SiteId(request.SiteId),
            Slug   = request.Slug,
        };

        var dto = await pipeline.RunAsync(ctx, cancellationToken);
        return Result.Success(dto);
    }
}
