using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MicroCMS.Api.Middleware;

/// <summary>
/// Suppresses the MVC form-value model-binders so ASP.NET Core does not
/// buffer (consume) a multipart/form-data request body before the controller
/// action gets a chance to read it with <see cref="Microsoft.AspNetCore.WebUtilities.MultipartReader"/>.
/// Apply this attribute to any streaming-upload action alongside
/// <see cref="Microsoft.AspNetCore.Mvc.DisableRequestSizeLimitAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var factories = context.ValueProviderFactories;
        factories.RemoveType<FormValueProviderFactory>();
        factories.RemoveType<FormFileValueProviderFactory>();
        factories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
