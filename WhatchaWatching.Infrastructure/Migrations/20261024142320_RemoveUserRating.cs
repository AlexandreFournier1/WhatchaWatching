using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatchaWatching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WatchListItems_UserScore",
                table: "WatchListItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WatchListItems");

            migrationBuilder.DropColumn(
                name: "UserScore",
                table: "WatchListItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WatchListItems",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte>(
                name: "UserScore",
                table: "WatchListItems",
                type: "tinyint unsigned",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_WatchListItems_UserScore",
                table: "WatchListItems",
                sql: "UserScore IS NULL OR UserScore BETWEEN 1 AND 10");
        }
    }
}
