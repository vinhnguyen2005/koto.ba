using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kotoba.Modules.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAndOwnerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("0276256f-13fb-4548-bcd8-caa1e6f68dbd"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("26380bd6-c644-4fb0-8e81-33059acdad02"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("2740e535-caca-488c-ab0a-6d67e3e4cd26"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("601c0bb0-8e24-4942-b33d-6666c6ea0e5a"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("6acc8208-d74f-47ab-abd1-6ac08e420425"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("8ed8639b-337f-44d7-8d4c-dc5648a5d58c"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("ae4b4e91-572e-49ba-bdce-25ae10a60bd8"));

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "ConversationParticipants",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "ConversationParticipants");

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("0276256f-13fb-4548-bcd8-caa1e6f68dbd"), null, 6, true, "Violence" },
                    { new Guid("26380bd6-c644-4fb0-8e81-33059acdad02"), null, 7, true, "Other" },
                    { new Guid("2740e535-caca-488c-ab0a-6d67e3e4cd26"), null, 1, true, "Spam" },
                    { new Guid("601c0bb0-8e24-4942-b33d-6666c6ea0e5a"), null, 4, true, "Harassment" },
                    { new Guid("6acc8208-d74f-47ab-abd1-6ac08e420425"), null, 5, true, "Misinformation" },
                    { new Guid("8ed8639b-337f-44d7-8d4c-dc5648a5d58c"), null, 3, true, "Adult content" },
                    { new Guid("ae4b4e91-572e-49ba-bdce-25ae10a60bd8"), null, 2, true, "Hate speech" }
                });
        }
    }
}
