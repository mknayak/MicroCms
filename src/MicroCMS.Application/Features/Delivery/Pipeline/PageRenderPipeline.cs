using MicroCMS.Application.Features.Delivery.Dtos;

namespace MicroCMS.Application.Features.Delivery.Pipeline;

/// <summary>
/// Composes registered <see cref="IPageRenderStep"/> implementations into a middleware chain
/// and runs them in registration order against a <see cref="PageRenderContext"/>.
///
/// Step ordering is determined by the order in which steps are registered in DI
/// (see <c>AddApplication()</c>). The first registered step is outermost (runs first).
///
/// Short-circuit: if any step sets <see cref="PageRenderContext.Result"/> without calling
/// <c>next()</c>, later steps are skipped and the result is returned immediately.
/// </summary>
public sealed class PageRenderPipeline(IEnumerable<IPageRenderStep> steps)
{
    private readonly IReadOnlyList<IPageRenderStep> _steps = steps.ToList();

    /// <summary>
    /// Runs the pipeline and returns the final <see cref="RenderedPageDto"/>.
    /// Throws <see cref="InvalidOperationException"/> if no step produced a result
    /// (i.e. neither <see cref="PageRenderContext.Result"/> nor <see cref="PageRenderContext.Html"/>
    /// were set by the time the chain finishes).
    /// </summary>
    public async Task<RenderedPageDto> RunAsync(PageRenderContext ctx, CancellationToken ct)
    {
        await BuildChain(0, ctx, ct)();

        if (ctx.Result is not null)
            return ctx.Result;

        // Build result from final context state (no short-circuit happened).
        if (ctx.Page is null)
            throw new InvalidOperationException("Pipeline completed without resolving a Page.");

        return new RenderedPageDto(
            ctx.Page.Id.Value,
            ctx.Page.Slug.Value,
            ctx.Page.Title,
            ctx.Html,
            ctx.Html is null ? ctx.Zones : null,
            ctx.Seo);
    }

    private Func<Task> BuildChain(int index, PageRenderContext ctx, CancellationToken ct)
    {
        if (index >= _steps.Count)
            return () => Task.CompletedTask;

        return () =>
        {
            // If a prior step short-circuited, stop walking.
            if (ctx.Result is not null)
                return Task.CompletedTask;

            return _steps[index].ExecuteAsync(ctx, BuildChain(index + 1, ctx, ct), ct);
        };
    }
}
