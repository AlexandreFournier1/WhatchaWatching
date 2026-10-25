using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatchaWatching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdultContentSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowAdultContent",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdult",
                table: "Media",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Every stored anime starts as "not adult". Marking them as never synchronized makes each one fetch its
            // data again, with the adult flag, the next time it is opened. Nothing is deleted.
            migrationBuilder.Sql("UPDATE `Media` SET `LastSyncedAt` = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowAdultContent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAdult",
                table: "Media");
        }
    }
}
