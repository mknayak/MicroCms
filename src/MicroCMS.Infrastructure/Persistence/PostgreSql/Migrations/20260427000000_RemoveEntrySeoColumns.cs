using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCMS.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEntrySeoColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SEO fields are only relevant for Page entities, not headless content entries.
            migrationBuilder.DropColumn(name: "SeoMetaTitle", table: "Entries");
            migrationBuilder.DropColumn(name: "SeoMetaDescription", table: "Entries");
            migrationBuilder.DropColumn(name: "SeoCanonicalUrl", table: "Entries");
            migrationBuilder.DropColumn(name: "SeoOgImage", table: "Entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
