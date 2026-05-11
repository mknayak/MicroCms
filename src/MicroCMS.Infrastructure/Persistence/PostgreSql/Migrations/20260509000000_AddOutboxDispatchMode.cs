using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCMS.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDispatchMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── OutboxMessages: add DispatchMode column ───────────────────
            migrationBuilder.AddColumn<int>(
                name: "DispatchMode",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0); // 0 = Exclusive

            // Replace old indexes with new ones that include DispatchMode.
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_TenantId_ProcessedOnUtc",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DispatchMode_ProcessedOnUtc_OccurredOnUtc",
                table: "OutboxMessages",
                columns: new[] { "DispatchMode", "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId_DispatchMode_ProcessedOnUtc",
                table: "OutboxMessages",
                columns: new[] { "TenantId", "DispatchMode", "ProcessedOnUtc" });

            // ── OutboxDeliveryRecords: new table ──────────────────────────
            migrationBuilder.CreateTable(
                name: "OutboxDeliveryRecords",
                columns: table => new
                {
                    MessageId      = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId     = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxDeliveryRecords", x => new { x.MessageId, x.InstanceId });
                    table.ForeignKey(
                        name: "FK_OutboxDeliveryRecords_OutboxMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeliveryRecords_MessageId",
                table: "OutboxDeliveryRecords",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OutboxDeliveryRecords");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_DispatchMode_ProcessedOnUtc_OccurredOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_TenantId_DispatchMode_ProcessedOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DispatchMode",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId_ProcessedOnUtc",
                table: "OutboxMessages",
                columns: new[] { "TenantId", "ProcessedOnUtc" });
        }
    }
}
