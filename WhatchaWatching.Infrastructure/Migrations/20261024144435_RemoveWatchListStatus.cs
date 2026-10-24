using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatchaWatching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWatchListStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WatchListItems_UserId_Status",
                table: "WatchListItems");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "WatchListItems");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "WatchListItems");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "WatchListItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WatchListItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "WatchListItems",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Progress",
                table: "WatchListItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "WatchListItems",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WatchListItems",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WatchListItems_UserId_Status",
                table: "WatchListItems",
                columns: new[] { "UserId", "Status" });
        }
    }
}
