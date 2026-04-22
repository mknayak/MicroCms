using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Ai;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Aggregates.Plugins;
using MicroCMS.Domain.Aggregates.Taxonomy;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Aggregates.Webhooks;
using MicroCMS.Infrastructure.Persistence.Common.Interceptors;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;

namespace MicroCMS.Infrastructure.Persistence.Common;

/// <summary>
/// The application's primary EF Core <see cref="DbContext"/>.
///
/// Multi-tenant isolation strategy — row-level security via global query filters:
/// The filter lambda references <c>_currentUser</c> (an instance field), so EF Core
/// re-evaluates <c>TenantId</c> on every query execution, correctly isolating each
/// HTTP request to its own tenant.
///
/// Security: EF Core uses parameterised queries exclusively. Raw SQL with string
/// concatenation is prohibited in this codebase. Bypassing filters via
/// <c>IgnoreQueryFilters()</c> is only permitted for SystemAdmin paths and must be
/// accompanied by an explicit role assertion at the call site.
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    // Nullable — null when invoked from design-time factory or background system jobs.
    // Query filters are only applied when a valid, authenticated current user is present.
    private readonly ICurrentUser? _currentUser;

    /// <summary>Design-time / migration constructor.</summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>Runtime constructor — receives the authenticated user for the current request scope.</summary>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUser currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    // ── DbSets ────────────────────────────────────────────────────────────

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSecuritySettings> TenantSecuritySettings => Set<TenantSecuritySettings>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<ApiClient> ApiClients => Set<ApiClient>();
    public DbSet<ContentType> ContentTypes => Set<ContentType>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaFolder> MediaFolders => Set<MediaFolder>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<PageTemplate> PageTemplates => Set<PageTemplate>();
    public DbSet<CopilotConversation> CopilotConversations => Set<CopilotConversation>();
    public DbSet<AiProviderSettings> AiProviderSettings => Set<AiProviderSettings>();
    public DbSet<Plugin> Plugins => Set<Plugin>();

    // ── Model configuration ───────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplyGlobalQueryFilters(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new DomainEventsToOutboxInterceptor());
    }

    // ── Multi-tenant query filters ────────────────────────────────────────

    /// <summary>
    /// Registers row-level tenant query filters on every tenant-scoped entity.
    ///
    /// IMPORTANT: the lambda captures <c>this._currentUser</c> by reference (because
    /// <c>_currentUser</c> is an instance field accessed via the implicit <c>this</c>).
    /// EF Core re-evaluates the lambda on every query, so each scope gets its own tenant scope.
    /// This is the recommended EF Core pattern for per-request query filters.
    ///
    /// When <c>_currentUser</c> is null or the user is not authenticated, the filter
    /// evaluates to <c>true</c>, meaning all rows are visible. This applies to:
    ///   - Design-time migrations
    ///   - System-admin background jobs that inject a null/anonymous user
    ///
    /// DO NOT use <c>IgnoreQueryFilters()</c> without verifying the <c>SystemAdmin</c> role.
    /// </summary>
    // Helper method to reduce cyclomatic complexity in ApplyGlobalQueryFilters
    private bool IsTenantVisible(TenantId tenantId)
    {
        return _currentUser == null || !_currentUser.IsAuthenticated || tenantId == _currentUser.TenantId;
    }

    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentType>().HasQueryFilter(ct => IsTenantVisible(ct.TenantId));
        modelBuilder.Entity<Entry>().HasQueryFilter(e => IsTenantVisible(e.TenantId));
        modelBuilder.Entity<Folder>().HasQueryFilter(f => IsTenantVisible(f.TenantId));
        modelBuilder.Entity<Page>().HasQueryFilter(p => IsTenantVisible(p.TenantId));
        modelBuilder.Entity<MediaAsset>().HasQueryFilter(a => IsTenantVisible(a.TenantId));
        modelBuilder.Entity<MediaFolder>().HasQueryFilter(f => IsTenantVisible(f.TenantId));
        modelBuilder.Entity<Category>().HasQueryFilter(c => IsTenantVisible(c.TenantId));
        modelBuilder.Entity<Tag>().HasQueryFilter(t => IsTenantVisible(t.TenantId));
        modelBuilder.Entity<User>().HasQueryFilter(u => IsTenantVisible(u.TenantId));
        modelBuilder.Entity<WebhookSubscription>().HasQueryFilter(w => IsTenantVisible(w.TenantId));
        modelBuilder.Entity<Component>().HasQueryFilter(c => IsTenantVisible(c.TenantId));
        modelBuilder.Entity<ApiClient>().HasQueryFilter(a => IsTenantVisible(a.TenantId));
        modelBuilder.Entity<TenantSecuritySettings>().HasQueryFilter(t => IsTenantVisible(t.TenantId));
    }
}
