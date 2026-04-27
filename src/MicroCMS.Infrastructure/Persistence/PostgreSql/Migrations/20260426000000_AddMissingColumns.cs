using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCMS.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Folders (content entry folders, GAP-02) ───────────────────────
            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId  = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId    = table.Column<Guid>(type: "uuid", nullable: false),
                    Name      = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Folders_Folders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Folders_TenantId_SiteId_ParentFolderId",
                table: "Folders",
                columns: new[] { "TenantId", "SiteId", "ParentFolderId" });

            // ── Entries: FolderId (GAP-02) ─────────────────────────────────
            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "Entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entries_FolderId",
                table: "Entries",
                column: "FolderId");

            // ── Entries: SEO metadata columns (GAP-08) ─────────────────────
            migrationBuilder.AddColumn<string>(
                name: "SeoMetaTitle",
                table: "Entries",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoMetaDescription",
                table: "Entries",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoCanonicalUrl",
                table: "Entries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoOgImage",
                table: "Entries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // ── ContentTypeFields: IsIndexed ───────────────────────────────
            migrationBuilder.AddColumn<bool>(
                name: "IsIndexed",
                table: "ContentTypeFields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // ── ContentTypes: LocalizationMode (DB-04) ─────────────────────
            migrationBuilder.AddColumn<string>(
                name: "LocalizationMode",
                table: "ContentTypes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PerLocale");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LocalizationMode",  table: "ContentTypes");
            migrationBuilder.DropColumn(name: "IsIndexed",          table: "ContentTypeFields");
            migrationBuilder.DropColumn(name: "SeoOgImage",         table: "Entries");
            migrationBuilder.DropColumn(name: "SeoCanonicalUrl",    table: "Entries");
            migrationBuilder.DropColumn(name: "SeoMetaDescription", table: "Entries");
            migrationBuilder.DropColumn(name: "SeoMetaTitle",       table: "Entries");
            migrationBuilder.DropIndex(name: "IX_Entries_FolderId", table: "Entries");
            migrationBuilder.DropColumn(name: "FolderId",           table: "Entries");
            migrationBuilder.DropTable(name: "Folders");
        }
    }
}
