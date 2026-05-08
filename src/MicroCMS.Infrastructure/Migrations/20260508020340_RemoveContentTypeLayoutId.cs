using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveContentTypeLayoutId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LayoutId",
                table: "ContentTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LayoutId",
                table: "ContentTypes",
                type: "TEXT",
                nullable: true);
        }
    }
}
