using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kotoba.Modules.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixStoryView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("4316eb1b-2cf9-4b17-8446-b333bda71662"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("4c8c60ed-6541-4990-8e52-dc0abff1281b"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("4d46c1db-8cb7-4f10-88c9-8a1852acb185"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("7696c409-4b4f-4df4-aed3-dd2a6174b127"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("82f78456-522a-42f8-8cf2-3f3cf0465380"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("832aa865-759a-4849-8194-a0c19838ae45"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("c2ad33b0-d028-4d1b-8aec-4d2bb1c2a5fc"));

            migrationBuilder.AddColumn<bool>(
                name: "NotificationSent",
                table: "StoryViews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("32d1ab77-fa41-4399-90f7-1b5a7539686c"), null, 5, true, "Misinformation" },
                    { new Guid("7f578411-dee6-4c8e-8f20-12842f7d6f2d"), null, 3, true, "Adult content" },
                    { new Guid("c34ae284-112d-4dd2-8272-1d0cf882454c"), null, 1, true, "Spam" },
                    { new Guid("d09a4cd8-3481-4723-911e-7a36f37d37d2"), null, 2, true, "Hate speech" },
                    { new Guid("d5784071-b6ff-4e76-a57f-cec4fc4326a4"), null, 4, true, "Harassment" },
                    { new Guid("da351c15-92c7-433c-9973-83c3b469bf9a"), null, 6, true, "Violence" },
                    { new Guid("e51d39c9-f0ec-494b-aaa4-beaddf75489f"), null, 7, true, "Other" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("32d1ab77-fa41-4399-90f7-1b5a7539686c"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("7f578411-dee6-4c8e-8f20-12842f7d6f2d"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("c34ae284-112d-4dd2-8272-1d0cf882454c"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("d09a4cd8-3481-4723-911e-7a36f37d37d2"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("d5784071-b6ff-4e76-a57f-cec4fc4326a4"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("da351c15-92c7-433c-9973-83c3b469bf9a"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("e51d39c9-f0ec-494b-aaa4-beaddf75489f"));

            migrationBuilder.DropColumn(
                name: "NotificationSent",
                table: "StoryViews");

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("4316eb1b-2cf9-4b17-8446-b333bda71662"), null, 1, true, "Spam" },
                    { new Guid("4c8c60ed-6541-4990-8e52-dc0abff1281b"), null, 5, true, "Misinformation" },
                    { new Guid("4d46c1db-8cb7-4f10-88c9-8a1852acb185"), null, 6, true, "Violence" },
                    { new Guid("7696c409-4b4f-4df4-aed3-dd2a6174b127"), null, 2, true, "Hate speech" },
                    { new Guid("82f78456-522a-42f8-8cf2-3f3cf0465380"), null, 3, true, "Adult content" },
                    { new Guid("832aa865-759a-4849-8194-a0c19838ae45"), null, 4, true, "Harassment" },
                    { new Guid("c2ad33b0-d028-4d1b-8aec-4d2bb1c2a5fc"), null, 7, true, "Other" }
                });
        }
    }
}
