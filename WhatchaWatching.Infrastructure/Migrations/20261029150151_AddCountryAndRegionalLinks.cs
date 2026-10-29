using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatchaWatching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryAndRegionalLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Users",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "StreamingLinks",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // The platforms stored so far are those of France, the default country of the site: they keep that country
            // until their title is fetched again. The links of an anime do not depend on the country.
            migrationBuilder.Sql(
                "UPDATE `StreamingLinks` SET `Region` = 'FR' WHERE `MediaId` IN (SELECT `Id` FROM `Media` WHERE `Type` IN ('Movie', 'TVShow'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "StreamingLinks");
        }
    }
}
