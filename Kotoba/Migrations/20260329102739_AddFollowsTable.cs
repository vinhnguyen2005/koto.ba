using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kotoba.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Follow_Users_FollowerId",
                table: "Follow");

            migrationBuilder.DropForeignKey(
                name: "FK_Follow_Users_FollowingId",
                table: "Follow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Follow",
                table: "Follow");

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("2843a75b-8093-40fe-8af3-5023ccdc4381"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("3650044c-feba-4fbc-997f-314856fc86a5"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("63d5a568-1c57-463c-88d1-345bb1c60c49"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("c354f10b-bcb2-4fb9-a94c-6f3645a9fb46"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("c7b68d7b-0818-41e4-823a-9bbf57bfac45"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("d88d3cd6-c437-4b83-bc0a-51dfff61d2a1"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("ddd0a80f-c7dc-431b-b2d3-147d825dec95"));

            migrationBuilder.RenameTable(
                name: "Follow",
                newName: "Follows");

            migrationBuilder.RenameIndex(
                name: "IX_Follow_FollowingId",
                table: "Follows",
                newName: "IX_Follows_FollowingId");

            migrationBuilder.RenameIndex(
                name: "IX_Follow_FollowerId",
                table: "Follows",
                newName: "IX_Follows_FollowerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Follows",
                table: "Follows",
                column: "Id");

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("0ea369d1-1f27-4368-9158-1075d454cc56"), null, 2, true, "Hate speech" },
                    { new Guid("9502a09b-8e92-42ca-a6eb-413bdb14b892"), null, 1, true, "Spam" },
                    { new Guid("a71d15f5-c9d4-490b-8568-d1bf54ff3f4f"), null, 7, true, "Other" },
                    { new Guid("a77a7cb9-dc09-4ddf-8d93-772315b7e65e"), null, 3, true, "Adult content" },
                    { new Guid("c90cb8b3-7ecd-4deb-a644-d5034a506532"), null, 6, true, "Violence" },
                    { new Guid("ec077b42-bbf0-4b66-ba8a-c55f2760e285"), null, 4, true, "Harassment" },
                    { new Guid("f4499cac-b57c-462f-9d23-664f24981a48"), null, 5, true, "Misinformation" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Follows_Users_FollowerId",
                table: "Follows",
                column: "FollowerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Follows_Users_FollowingId",
                table: "Follows",
                column: "FollowingId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Follows_Users_FollowerId",
                table: "Follows");

            migrationBuilder.DropForeignKey(
                name: "FK_Follows_Users_FollowingId",
                table: "Follows");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Follows",
                table: "Follows");

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("0ea369d1-1f27-4368-9158-1075d454cc56"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("9502a09b-8e92-42ca-a6eb-413bdb14b892"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("a71d15f5-c9d4-490b-8568-d1bf54ff3f4f"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("a77a7cb9-dc09-4ddf-8d93-772315b7e65e"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("c90cb8b3-7ecd-4deb-a644-d5034a506532"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("ec077b42-bbf0-4b66-ba8a-c55f2760e285"));

            migrationBuilder.DeleteData(
                table: "ReportCategories",
                keyColumn: "Id",
                keyValue: new Guid("f4499cac-b57c-462f-9d23-664f24981a48"));

            migrationBuilder.RenameTable(
                name: "Follows",
                newName: "Follow");

            migrationBuilder.RenameIndex(
                name: "IX_Follows_FollowingId",
                table: "Follow",
                newName: "IX_Follow_FollowingId");

            migrationBuilder.RenameIndex(
                name: "IX_Follows_FollowerId",
                table: "Follow",
                newName: "IX_Follow_FollowerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Follow",
                table: "Follow",
                column: "Id");

            migrationBuilder.InsertData(
                table: "ReportCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("2843a75b-8093-40fe-8af3-5023ccdc4381"), null, 2, true, "Hate speech" },
                    { new Guid("3650044c-feba-4fbc-997f-314856fc86a5"), null, 1, true, "Spam" },
                    { new Guid("63d5a568-1c57-463c-88d1-345bb1c60c49"), null, 3, true, "Adult content" },
                    { new Guid("c354f10b-bcb2-4fb9-a94c-6f3645a9fb46"), null, 7, true, "Other" },
                    { new Guid("c7b68d7b-0818-41e4-823a-9bbf57bfac45"), null, 4, true, "Harassment" },
                    { new Guid("d88d3cd6-c437-4b83-bc0a-51dfff61d2a1"), null, 6, true, "Violence" },
                    { new Guid("ddd0a80f-c7dc-431b-b2d3-147d825dec95"), null, 5, true, "Misinformation" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Follow_Users_FollowerId",
                table: "Follow",
                column: "FollowerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Follow_Users_FollowingId",
                table: "Follow",
                column: "FollowingId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
