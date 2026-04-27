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
using MicroCMS.Domain.Aggregates.Locks;

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
    private readonly ICurrentUser? _currentUser;

    /// <summary>
    /// Resolved once per context instance. <c>null</c> means "no filter" (migrations, background jobs).
    /// EF Core captures this field reference and re-evaluates per query.
    /// Using a single nullable field rather than a multi-operand expression keeps each
    /// query-filter lambda to a single equality check, staying within complexity limits.
    /// </summary>
    private readonly TenantId? _tenantFilter;

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
        // Only apply tenant isolation when there is a real authenticated user.
        _tenantFilter = currentUser.IsAuthenticated ? currentUser.TenantId : null;
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
    public DbSet<ComponentItem> ComponentItems => Set<ComponentItem>();
    public DbSet<PageTemplate> PageTemplates => Set<PageTemplate>();
    public DbSet<CopilotConversation> CopilotConversations => Set<CopilotConversation>();
    public DbSet<AiProviderSettings> AiProviderSettings => Set<AiProviderSettings>();
    public DbSet<Plugin> Plugins => Set<Plugin>();
    public DbSet<Layout> Layouts => Set<Layout>();
    public DbSet<EditLock> EditLocks => Set<EditLock>();
    public DbSet<SiteTemplate> SiteTemplates => Set<SiteTemplate>();

    // ── Model configuration ───────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplyGlobalQueryFilters(modelBuilder);
        ApplySqliteDateTimeOffsetConverters(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new DomainEventsToOutboxInterceptor());
    }

    // ── Multi-tenant query filters ────────────────────────────────────────

    /// <summary>
    /// Registers row-level tenant query filters on every tenant-scoped entity.
    /// Split into focused methods to keep cyclomatic complexity within the project limit.
    ///
    /// IMPORTANT: the lambda captures <c>this._currentUser</c> by reference.
    /// EF Core re-evaluates the lambda on every query execution.
    /// When <c>_currentUser</c> is null or unauthenticated all rows are visible (migrations, background jobs).
    ///
    /// DO NOT extract the filter condition to a helper method — EF Core cannot translate
    /// instance method calls to SQL. The expression must be inlined in each lambda.
    /// </summary>
    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        ApplyContentFilters(modelBuilder);
        ApplyMediaFilters(modelBuilder);
        ApplyIdentityAndSecurityFilters(modelBuilder);
        ApplyTaxonomyFilters(modelBuilder);
    }

    private void ApplyContentFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentType>().HasQueryFilter(ct => _tenantFilter == null || ct.TenantId == _tenantFilter);
        modelBuilder.Entity<Entry>().HasQueryFilter(e => _tenantFilter == null || e.TenantId == _tenantFilter);
        modelBuilder.Entity<Folder>().HasQueryFilter(f => _tenantFilter == null || f.TenantId == _tenantFilter);
        modelBuilder.Entity<Page>().HasQueryFilter(p => _tenantFilter == null || p.TenantId == _tenantFilter);
        modelBuilder.Entity<Component>().HasQueryFilter(c => _tenantFilter == null || c.TenantId == _tenantFilter);
        modelBuilder.Entity<ComponentItem>().HasQueryFilter(ci => _tenantFilter == null || ci.TenantId == _tenantFilter);
        modelBuilder.Entity<Layout>().HasQueryFilter(l => _tenantFilter == null || l.TenantId == _tenantFilter);
        modelBuilder.Entity<SiteTemplate>().HasQueryFilter(st => _tenantFilter == null || st.TenantId == _tenantFilter);
        // EditLock is not tenant-scoped — locks are global per entity ID
    }

    private void ApplyMediaFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaAsset>().HasQueryFilter(a => _tenantFilter == null || a.TenantId == _tenantFilter);
        modelBuilder.Entity<MediaFolder>().HasQueryFilter(f => _tenantFilter == null || f.TenantId == _tenantFilter);
    }

    private void ApplyIdentityAndSecurityFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasQueryFilter(u => _tenantFilter == null || u.TenantId == _tenantFilter);
        modelBuilder.Entity<ApiClient>().HasQueryFilter(a => _tenantFilter == null || a.TenantId == _tenantFilter);
        modelBuilder.Entity<TenantSecuritySettings>().HasQueryFilter(t => _tenantFilter == null || t.TenantId == _tenantFilter);
        modelBuilder.Entity<WebhookSubscription>().HasQueryFilter(w => _tenantFilter == null || w.TenantId == _tenantFilter);
    }

    private void ApplyTaxonomyFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasQueryFilter(c => _tenantFilter == null || c.TenantId == _tenantFilter);
        modelBuilder.Entity<Tag>().HasQueryFilter(t => _tenantFilter == null || t.TenantId == _tenantFilter);
    }

    /// <summary>
    /// SQLite cannot ORDER BY or compare <see cref="DateTimeOffset"/> columns stored as TEXT.
    /// This method replaces every <see cref="DateTimeOffset"/> property with a
    /// <c>long</c> (UTC ticks) representation so SQLite can sort them correctly.
    /// This is a no-op on other providers (PostgreSQL, SQL Server) where
    /// <see cref="DateTimeOffset"/> is a native type.
    /// </summary>
    private void ApplySqliteDateTimeOffsetConverters(ModelBuilder modelBuilder)
    {
        if (!Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ?? true)
            return;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            ApplySqliteConvertersToEntity(entityType);
    }

    private static void ApplySqliteConvertersToEntity(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
            ApplySqliteConverterToProperty(property);
    }

    private static void ApplySqliteConverterToProperty(Microsoft.EntityFrameworkCore.Metadata.IMutableProperty property)
    {
        if (property.ClrType == typeof(DateTimeOffset))
        {
            property.SetValueConverter(
                new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(
                    dto => dto.UtcTicks,
                    ticks => new DateTimeOffset(ticks, TimeSpan.Zero)));
        }
        else if (property.ClrType == typeof(DateTimeOffset?))
        {
            property.SetValueConverter(
                new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, long?>(
                    dto => dto == null ? null : dto.Value.UtcTicks,
                    ticks => ticks == null ? null : new DateTimeOffset(ticks.Value, TimeSpan.Zero)));
        }
    }
}
