namespace MicroCMS.Application.Features.Delivery.Pipeline;

/// <summary>
/// A single step in the page render pipeline.
///
/// Steps are executed in registration order by <see cref="PageRenderPipeline"/>.
/// Each step receives the shared <see cref="PageRenderContext"/>, enriches it,
/// and calls <paramref name="next"/> to continue — unless it short-circuits by
/// setting <see cref="PageRenderContext.Result"/> and returning without calling next.
///
/// Implementing a step:
/// <code>
/// internal sealed class MyStep : IPageRenderStep
/// {
///     public async Task ExecuteAsync(PageRenderContext ctx, Func&lt;Task&gt; next, CancellationToken ct)
///     {
///         // ... enrich ctx ...
///         await next(); // pass control to the following step
///     }
/// }
/// </code>
/// </summary>
public interface IPageRenderStep
{
    /// <summary>
    /// Executes this step.
    /// Call <paramref name="next"/> to proceed to the next step.
    /// Omit the call to <paramref name="next"/> to short-circuit the pipeline.
    /// </summary>
    Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct);
}
