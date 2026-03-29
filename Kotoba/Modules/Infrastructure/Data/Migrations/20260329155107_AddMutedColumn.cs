using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kotoba.Modules.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMutedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("03385eb1-2385-4a85-ab3b-d495dfda7084"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("5bbc4d6d-81f3-4890-bb7e-e901be1bc6f3"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("6b2977b7-46b9-41a2-9857-659fe8ef4991"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("6e07c934-460c-4304-92b1-2ea4134e1c2a"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("77cbee9b-f77a-4b27-bd58-3d1006a58128"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("cfd12c27-0677-487a-b587-75f5d99e138d"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("d6f519cd-1fe8-4410-89fb-f9f05671e6bb"));

            migrationBuilder.AddColumn<bool>(
                name: "IsMuted",
                table: "ConversationParticipants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("09b81f44-3c74-4590-be98-0e039d895917"), null, 5, true, "Misinformation" },
                    { new Guid("1492e272-2c27-4593-8f88-722ba92a900b"), null, 1, true, "Spam" },
                    { new Guid("81af1536-10b8-4d53-958a-54b92f2bc681"), null, 7, true, "Other" },
                    { new Guid("a29408f9-cd3c-4faa-8936-540b42ef0f98"), null, 6, true, "Violence" },
                    { new Guid("ab06fcf0-c1e1-47ae-a37f-828113618b39"), null, 2, true, "Hate speech" },
                    { new Guid("c0c942a8-fb51-4d86-b9e2-6533a6f24372"), null, 4, true, "Harassment" },
                    { new Guid("f425f62d-8c08-4007-be10-ee2fe93f5b63"), null, 3, true, "Adult content" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("09b81f44-3c74-4590-be98-0e039d895917"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("1492e272-2c27-4593-8f88-722ba92a900b"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("81af1536-10b8-4d53-958a-54b92f2bc681"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("a29408f9-cd3c-4faa-8936-540b42ef0f98"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("ab06fcf0-c1e1-47ae-a37f-828113618b39"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("c0c942a8-fb51-4d86-b9e2-6533a6f24372"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("f425f62d-8c08-4007-be10-ee2fe93f5b63"));

            migrationBuilder.DropColumn(
                name: "IsMuted",
                table: "ConversationParticipants");

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("03385eb1-2385-4a85-ab3b-d495dfda7084"), null, 2, true, "Hate speech" },
                    { new Guid("5bbc4d6d-81f3-4890-bb7e-e901be1bc6f3"), null, 6, true, "Violence" },
                    { new Guid("6b2977b7-46b9-41a2-9857-659fe8ef4991"), null, 3, true, "Adult content" },
                    { new Guid("6e07c934-460c-4304-92b1-2ea4134e1c2a"), null, 4, true, "Harassment" },
                    { new Guid("77cbee9b-f77a-4b27-bd58-3d1006a58128"), null, 1, true, "Spam" },
                    { new Guid("cfd12c27-0677-487a-b587-75f5d99e138d"), null, 7, true, "Other" },
                    { new Guid("d6f519cd-1fe8-4410-89fb-f9f05671e6bb"), null, 5, true, "Misinformation" }
                });
        }
    }
}
