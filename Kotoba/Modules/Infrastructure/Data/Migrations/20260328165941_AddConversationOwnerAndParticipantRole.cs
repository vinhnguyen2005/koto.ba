using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kotoba.Modules.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationOwnerAndParticipantRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Conversations', 'OwnerId') IS NULL
BEGIN
    ALTER TABLE [Conversations] ADD [OwnerId] nvarchar(max) NULL;
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('ConversationParticipants', 'Role') IS NULL
BEGIN
    ALTER TABLE [ConversationParticipants] ADD [Role] int NOT NULL CONSTRAINT [DF_ConversationParticipants_Role] DEFAULT(0);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Conversations', 'OwnerId') IS NOT NULL
BEGIN
    ALTER TABLE [Conversations] DROP COLUMN [OwnerId];
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('ConversationParticipants', 'Role') IS NOT NULL
BEGIN
    DECLARE @RoleDefaultConstraint nvarchar(128);
    SELECT @RoleDefaultConstraint = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = 'ConversationParticipants' AND c.name = 'Role';

    IF @RoleDefaultConstraint IS NOT NULL
        EXEC('ALTER TABLE [ConversationParticipants] DROP CONSTRAINT [' + @RoleDefaultConstraint + ']');

    ALTER TABLE [ConversationParticipants] DROP COLUMN [Role];
END");
        }
    }
}
