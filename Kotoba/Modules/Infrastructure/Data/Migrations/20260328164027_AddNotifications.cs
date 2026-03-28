using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kotoba.Modules.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("1a170318-2f4f-4325-aeea-6073630df0d1"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("4926ed3e-78e1-470a-8a03-28bceebd24ba"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("8a4b8cc4-09d7-4faf-9327-099c8238ce19"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("9dfff7de-3bd3-4766-91cd-4aa04098ff87"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("a90e4cc7-b382-40d3-bda1-fdb091a19c66"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("c81c599a-0de1-4156-aab8-bdd45658f3d3"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("e3110a2e-f503-4585-9262-8ae741ac90d1"));

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TargetId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("165dc0a5-eab1-4d0f-bcd4-39d5cc8fbf06"), null, 5, true, "Misinformation" },
                    { new Guid("49350044-188e-4df7-89a8-dc9a56705c33"), null, 3, true, "Adult content" },
                    { new Guid("65713a39-4217-4779-890c-28d286ab0895"), null, 2, true, "Hate speech" },
                    { new Guid("839838a9-2133-43d7-8872-2eb13187ae0b"), null, 4, true, "Harassment" },
                    { new Guid("8850cd92-caa4-4732-acd3-ab9437692142"), null, 7, true, "Other" },
                    { new Guid("ab79eef2-9c0b-4e92-a0cd-c05405de5720"), null, 1, true, "Spam" },
                    { new Guid("e9a20003-ebf3-4661-befb-bc4697a08f61"), null, 6, true, "Violence" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ActorId",
                table: "Notifications",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId",
                table: "Notifications",
                column: "RecipientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("165dc0a5-eab1-4d0f-bcd4-39d5cc8fbf06"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("49350044-188e-4df7-89a8-dc9a56705c33"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("65713a39-4217-4779-890c-28d286ab0895"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("839838a9-2133-43d7-8872-2eb13187ae0b"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("8850cd92-caa4-4732-acd3-ab9437692142"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("ab79eef2-9c0b-4e92-a0cd-c05405de5720"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("e9a20003-ebf3-4661-befb-bc4697a08f61"));

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("1a170318-2f4f-4325-aeea-6073630df0d1"), null, 3, true, "Adult content" },
                    { new Guid("4926ed3e-78e1-470a-8a03-28bceebd24ba"), null, 5, true, "Misinformation" },
                    { new Guid("8a4b8cc4-09d7-4faf-9327-099c8238ce19"), null, 1, true, "Spam" },
                    { new Guid("9dfff7de-3bd3-4766-91cd-4aa04098ff87"), null, 7, true, "Other" },
                    { new Guid("a90e4cc7-b382-40d3-bda1-fdb091a19c66"), null, 4, true, "Harassment" },
                    { new Guid("c81c599a-0de1-4156-aab8-bdd45658f3d3"), null, 6, true, "Violence" },
                    { new Guid("e3110a2e-f503-4585-9262-8ae741ac90d1"), null, 2, true, "Hate speech" }
                });
        }
    }
}
