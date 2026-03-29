using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kotoba.Modules.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyToCurrentThought : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<int>(
                name: "Privacy",
                table: "CurrentThoughts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Follows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FollowingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Follows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Follows_Users_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Follows_Users_FollowingId",
                        column: x => x.FollowingId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("1394bb6a-6ee2-4c3a-b783-89b34ccdc777"), null, 1, true, "Spam" },
                    { new Guid("2e0cf79b-ecfe-431c-86bb-e290aec83f84"), null, 3, true, "Adult content" },
                    { new Guid("6864dbed-2aee-4ff0-9cf8-6ef3ad67a027"), null, 2, true, "Hate speech" },
                    { new Guid("6f4012f1-34db-410b-a188-61df65ff4b46"), null, 6, true, "Violence" },
                    { new Guid("c9ee6653-0a11-4f11-a60d-a7b759f1d10d"), null, 4, true, "Harassment" },
                    { new Guid("cfc3c064-12f7-4337-83e2-180db722d99a"), null, 7, true, "Other" },
                    { new Guid("d8d9e043-e9d2-45bd-907d-4ae354c8eec9"), null, 5, true, "Misinformation" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FollowerId",
                table: "Follows",
                column: "FollowerId");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FollowingId",
                table: "Follows",
                column: "FollowingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Follows");

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("1394bb6a-6ee2-4c3a-b783-89b34ccdc777"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("2e0cf79b-ecfe-431c-86bb-e290aec83f84"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("6864dbed-2aee-4ff0-9cf8-6ef3ad67a027"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("6f4012f1-34db-410b-a188-61df65ff4b46"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("c9ee6653-0a11-4f11-a60d-a7b759f1d10d"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("cfc3c064-12f7-4337-83e2-180db722d99a"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("d8d9e043-e9d2-45bd-907d-4ae354c8eec9"));

            migrationBuilder.DropColumn(
                name: "Privacy",
                table: "CurrentThoughts");

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
        }
    }
}
