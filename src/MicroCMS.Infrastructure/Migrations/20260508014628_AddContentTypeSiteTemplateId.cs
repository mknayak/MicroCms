using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentTypeSiteTemplateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SiteTemplateId",
                table: "ContentTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntryGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ImageAssetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntryGroupMembers",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryGroupMembers", x => new { x.GroupId, x.EntryId });
                    table.ForeignKey(
                        name: "FK_EntryGroupMembers_EntryGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "EntryGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntryGroups_SiteId_ContentTypeId_Handle",
                table: "EntryGroups",
                columns: new[] { "SiteId", "ContentTypeId", "Handle" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntryGroupMembers");

            migrationBuilder.DropTable(
                name: "EntryGroups");

            migrationBuilder.DropColumn(
                name: "SiteTemplateId",
                table: "ContentTypes");
        }
    }
}
