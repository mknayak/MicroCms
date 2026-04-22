using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCMS.Infrastructure.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Tenants ───────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 63, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Settings_DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Settings_DefaultLocale = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    Settings_EnabledLocales = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Settings_TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Settings_AiEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Settings_LogoUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Quota_MaxStorageBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Quota_MaxApiCallsPerMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    Quota_MaxUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    Quota_MaxSites = table.Column<int>(type: "INTEGER", nullable: false),
                    Quota_MaxContentTypes = table.Column<int>(type: "INTEGER", nullable: false),
                    Quota_MaxAiTokensPerMonth = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Tenants", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            // ── Sites ─────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DefaultLocale = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    CustomDomain = table.Column<string>(type: "TEXT", maxLength: 253, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sites_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_TenantId_Handle",
                table: "Sites",
                columns: new[] { "TenantId", "Handle" },
                unique: true);

            // ── ContentTypes ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ContentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ContentTypes", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_SiteId_Handle",
                table: "ContentTypes",
                columns: new[] { "SiteId", "Handle" },
                unique: true);

            // ── ContentTypeFields ─────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ContentTypeFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocalized = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnique = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ValidationJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypeFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_ContentTypeId_Handle",
                table: "ContentTypeFields",
                columns: new[] { "ContentTypeId", "Handle" },
                unique: true);

            // ── Entries ───────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Locale = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CurrentVersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ScheduledPublishAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ScheduledUnpublishAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Entries", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Entries_SiteId_Locale_Slug",
                table: "Entries",
                columns: new[] { "SiteId", "Locale", "Slug" },
                unique: true);

            // ── EntryVersions ─────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "EntryVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangeNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntryVersions_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntryVersions_EntryId_VersionNumber",
                table: "EntryVersions",
                columns: new[] { "EntryId", "VersionNumber" },
                unique: true);

            // ── MediaAssets ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StorageKey = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    FolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UploadedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AltText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Meta_FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Meta_MimeType = table.Column<string>(type: "TEXT", maxLength: 127, nullable: false),
                    Meta_SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Meta_WidthPx = table.Column<int>(type: "TEXT", nullable: true),
                    Meta_HeightPx = table.Column<int>(type: "TEXT", nullable: true),
                    Meta_Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Meta_ExifJson = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false, defaultValue: "")
                },
                constraints: table => table.PrimaryKey("PK_MediaAssets", x => x.Id));

            // ── MediaFolders ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "MediaFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_MediaFolders", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_MediaFolders_TenantId_SiteId_ParentFolderId",
                table: "MediaFolders",
                columns: new[] { "TenantId", "SiteId", "ParentFolderId" });

            // ── Categories ────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Categories", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Categories_SiteId_Slug",
                table: "Categories",
                columns: new[] { "SiteId", "Slug" },
                unique: true);

            // ── Tags ──────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Tags", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Tags_SiteId_Slug",
                table: "Tags",
                columns: new[] { "SiteId", "Slug" },
                unique: true);

            // ── Users ─────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            // ── UserRoles ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowRole = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── OutboxMessages ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table => table.PrimaryKey("PK_OutboxMessages", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId_ProcessedOnUtc",
                table: "OutboxMessages",
                columns: new[] { "TenantId", "ProcessedOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OutboxMessages");
            migrationBuilder.DropTable(name: "UserRoles");
            migrationBuilder.DropTable(name: "Users");
            migrationBuilder.DropTable(name: "Tags");
            migrationBuilder.DropTable(name: "Categories");
            migrationBuilder.DropTable(name: "MediaFolders");
            migrationBuilder.DropTable(name: "MediaAssets");
            migrationBuilder.DropTable(name: "EntryVersions");
            migrationBuilder.DropTable(name: "Entries");
            migrationBuilder.DropTable(name: "ContentTypeFields");
            migrationBuilder.DropTable(name: "ContentTypes");
            migrationBuilder.DropTable(name: "Sites");
            migrationBuilder.DropTable(name: "Tenants");
        }
    }
}
