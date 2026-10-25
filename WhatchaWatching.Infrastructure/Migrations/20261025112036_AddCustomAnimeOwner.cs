using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatchaWatching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomAnimeOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Media",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_CreatedByUserId",
                table: "Media",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Users_CreatedByUserId",
                table: "Media",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_Users_CreatedByUserId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_CreatedByUserId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Media");
        }
    }
}
