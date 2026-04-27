using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Services;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Infrastructure.Tenancy;

/// <summary>
/// Provisions a new tenant atomically within the current unit of work:
///   1. Creates and persists the <see cref="Tenant"/> aggregate (Provisioning → Active).
///   2. Adds a default site.
///   3. Creates the admin <see cref="User"/> and assigns <c>TenantAdmin</c> role.
///   4. Seeds a built-in <c>page</c> ContentType with standard page-data fields.
///
/// All steps share the same <see cref="ApplicationDbContext"/> scope so the
/// caller's UnitOfWorkBehavior commits everything in a single transaction.
/// </summary>
internal sealed class TenantOnboardingService(
    IRepository<Tenant, TenantId> tenantRepo,
    IRepository<User, UserId> userRepo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo)
    : ITenantOnboardingService
{
    public async Task<TenantOnboardingResult> OnboardAsync(
        TenantOnboardingRequest request,
     CancellationToken cancellationToken = default)
    {
        var locale = Locale.Create(request.DefaultLocale);

        var settings = TenantSettings.Create(
   request.DisplayName, locale,
     timeZoneId: request.TimeZoneId);

        var slug = TenantSlug.Create(request.Slug);
   var tenant = Tenant.Create(slug, settings);
  tenant.Activate();

      var site = tenant.AddSite(
    request.DefaultSiteName,
            Slug.Create("main"),
     locale);

        await tenantRepo.AddAsync(tenant, cancellationToken);

        // Create admin user scoped to the new tenant
        var adminEmail = EmailAddress.Create(request.AdminEmail);
        var adminName = PersonName.Create(request.AdminDisplayName);
        var adminUser = User.Create(tenant.Id, adminEmail, adminName);
        adminUser.AssignRole(WorkflowRole.TenantAdmin, Roles.TenantAdmin);
        await userRepo.AddAsync(adminUser, cancellationToken);

        // Seed the built-in "page" ContentType for this site
        await SeedPageContentTypeAsync(tenant.Id, site.Id, cancellationToken);

        return new TenantOnboardingResult(
  tenant.Id.Value,
     site.Id.Value,
 adminUser.Id.Value);
    }

    // ── Seeding ───────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the built-in <c>page</c> ContentType that backs Static page data.
    ///
    /// Fields:
    ///   - title (ShortText, required) — page heading / browser tab title
    ///- description (LongText) — page summary / intro paragraph
    /// - heroImage (ShortText / URL) — hero image URL or asset reference handle
    ///   - customCss (LongText) — page-scoped CSS injected into the layout &lt;head&gt;
    ///   - customJs (LongText) — page-scoped JS injected before &lt;/body&gt;
    ///   - canonicalUrl (ShortText) — explicit canonical URL override
    ///
    /// Editors create an Entry of this ContentType and link it to a Static Page via
    /// the page's <c>linkedEntryId</c>. The render pipeline picks up the entry's
    /// <c>SeoMetadata</c> and its field data can be referenced in component templates
    /// or accessed via the Delivery API.
    /// </summary>
  private async Task SeedPageContentTypeAsync(
  TenantId tenantId, SiteId siteId, CancellationToken ct)
    {
      var ct_ = ContentType.Create(
     tenantId, siteId,
      handle: "page",
       displayName: "Page",
            description: "Built-in content type for Static page data (title, description, hero image, custom CSS/JS).");

        ct_.AddField("title",     "Title",       FieldType.ShortText, isRequired: true,
      description: "Page heading used as the browser tab title and layout {{seo:title}} token.");
      ct_.AddField("description", "Description", FieldType.LongText,
       description: "Page summary rendered as the meta description and intro paragraph.");
        ct_.AddField("heroImage",   "Hero Image",  FieldType.ShortText,
   description: "URL or asset handle for the page hero image.");
        ct_.AddField("customCss",   "Custom CSS",  FieldType.LongText,
       description: "Page-scoped CSS injected into the layout <head>. Wrap in <style> tags.");
        ct_.AddField("customJs",    "Custom JS",   FieldType.LongText,
            description: "Page-scoped JavaScript injected before </body>. Wrap in <script> tags.");
        ct_.AddField("canonicalUrl","Canonical URL",FieldType.ShortText,
            description: "Explicit canonical URL override for SEO deduplication.");

   ct_.Publish();

     await contentTypeRepo.AddAsync(ct_, ct);
  }
}
