using Microsoft.Extensions.DependencyInjection;
using MicroCMS.Ai.Abstractions.Interfaces;
using MicroCMS.Ai.Core.Services;

namespace MicroCMS.Ai.Core;

/// <summary>
/// Dependency injection extensions for AI Core module.
/// Registers all AI services. Providers are registered separately by the composition root.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds AI Core services to the service collection.
    /// Registers orchestrator, provider registry, budget service, redactor, prompt library, and validator.
    /// Provider registration should be done separately by calling methods on ProviderRegistry.
    /// </summary>
    public static IServiceCollection AddAiCore(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<ProviderRegistry>();
        services.AddScoped<AiOrchestrator>();
        services.AddScoped<BudgetService>();
        services.AddScoped<PiiRedactor>();
        services.AddScoped<PromptLibrary>();
        services.AddSingleton<StructuredOutputValidator>();

        // Register as IAiUsageTracker
        services.AddScoped<IAiUsageTracker>(sp => sp.GetRequiredService<BudgetService>());

        return services;
    }
}
