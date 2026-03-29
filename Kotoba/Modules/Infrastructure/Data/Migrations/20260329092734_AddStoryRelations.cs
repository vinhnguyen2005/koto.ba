using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kotoba.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryRelations : Migration
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

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Stories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "StoryPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryPermissions_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoryPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoryReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryReactions_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoryReactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoryViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryViews_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoryViews_Users_ViewerId",
                        column: x => x.ViewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_StoryPermissions_StoryId_UserId",
                table: "StoryPermissions",
                columns: new[] { "StoryId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryPermissions_UserId",
                table: "StoryPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryReactions_StoryId_UserId",
                table: "StoryReactions",
                columns: new[] { "StoryId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryReactions_UserId",
                table: "StoryReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryViews_StoryId_ViewerId",
                table: "StoryViews",
                columns: new[] { "StoryId", "ViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryViews_ViewerId",
                table: "StoryViews",
                column: "ViewerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoryPermissions");

            migrationBuilder.DropTable(
                name: "StoryReactions");

            migrationBuilder.DropTable(
                name: "StoryViews");

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

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Stories");

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
