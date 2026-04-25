using HotChocolate.Execution.Configuration;
using MediatR;
using MicroCMS.Application.Common.Events;
using MicroCMS.GraphQL.DataLoaders;
using MicroCMS.GraphQL.Mutations;
using MicroCMS.GraphQL.Queries;
using MicroCMS.GraphQL.Schema;
using MicroCMS.GraphQL.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using DomainEntryPublishedEvent = MicroCMS.Domain.Events.Content.EntryPublishedEvent;

namespace MicroCMS.GraphQL;

/// <summary>
/// DI extension methods for the GraphQL layer.
/// Called from the host project's <c>AddGraphQlServices</c> helper.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Hot Chocolate schema, resolvers, data loaders, subscriptions,
    /// persisted queries, and the MediatR→subscription bridge.
    /// </summary>
    public static IRequestExecutorBuilder AddGraphQlSchema(
        this IServiceCollection services)
    {
        // Bridge: forwards EntryPublishedEvent domain events to Hot Chocolate subscription topics.
        services.AddScoped<
            INotificationHandler<DomainEventNotification<DomainEntryPublishedEvent>>,
            EntryPublishedSubscriptionBridge>();

        // Dynamic schema service — translates ContentType field definitions to GraphQL scalars.
        services.AddSingleton<DynamicSchemaService>();

        return services
            .AddGraphQLServer()
            .AddQueryType<RootQuery>()
            .AddMutationType<RootMutation>()
            .AddSubscriptionType<RootSubscription>()
            // Computed fields contributed to EntryDto at schema level.
            .AddTypeExtension<DynamicEntryTypeExtension>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddAuthorization()
            .AddInMemorySubscriptions()
            .AddDataLoader<EntryByIdDataLoader>()
            .AddDataLoader<MediaAssetByIdDataLoader>()
            // Persisted queries: clients send a query hash instead of the full document.
            .UsePersistedQueryPipeline()
            .AddReadOnlyFileSystemQueryStorage("./persisted-queries")
            .ModifyRequestOptions(opt =>
            {
                // Security: never expose internal exception details in GraphQL error responses.
                opt.IncludeExceptionDetails = false;
            })
            // Security: reject queries deeper than 10 levels to prevent denial-of-service via nesting.
            .AddMaxExecutionDepthRule(10, skipIntrospectionFields: true);
    }
}
