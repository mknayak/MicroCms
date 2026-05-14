using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAssetPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetPath",
                table: "MediaAssets",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_TenantId_SiteId_AssetPath",
                table: "MediaAssets",
                columns: new[] { "TenantId", "SiteId", "AssetPath" },
                unique: true,
                filter: "\"AssetPath\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_TenantId_SiteId_AssetPath",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "AssetPath",
                table: "MediaAssets");
        }
    }
}
