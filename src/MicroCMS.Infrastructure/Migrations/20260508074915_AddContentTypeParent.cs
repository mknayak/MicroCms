using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentTypeParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentContentTypeId",
                table: "ContentTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_ParentContentTypeId",
                table: "ContentTypes",
                column: "ParentContentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentTypes_ContentTypes_ParentContentTypeId",
                table: "ContentTypes",
                column: "ParentContentTypeId",
                principalTable: "ContentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentTypes_ContentTypes_ParentContentTypeId",
                table: "ContentTypes");

            migrationBuilder.DropIndex(
                name: "IX_ContentTypes_ParentContentTypeId",
                table: "ContentTypes");

            migrationBuilder.DropColumn(
                name: "ParentContentTypeId",
                table: "ContentTypes");
        }
    }
}
