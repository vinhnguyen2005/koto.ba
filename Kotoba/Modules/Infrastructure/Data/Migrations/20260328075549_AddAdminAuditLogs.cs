using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kotoba.Modules.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    TargetEntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TargetEntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_Users_PerformedByAdminId",
                        column: x => x.PerformedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_ActionType_TimestampUtc",
                table: "AdminAuditLogs",
                columns: new[] { "ActionType", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_PerformedByAdminId_TimestampUtc",
                table: "AdminAuditLogs",
                columns: new[] { "PerformedByAdminId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_TimestampUtc",
                table: "AdminAuditLogs",
                column: "TimestampUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");
        }
    }
}
