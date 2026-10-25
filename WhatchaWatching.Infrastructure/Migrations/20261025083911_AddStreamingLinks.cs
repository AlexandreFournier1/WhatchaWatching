using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatchaWatching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStreamingLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StreamingLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MediaId = table.Column<int>(type: "int", nullable: false),
                    Site = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamingLinks_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingLinks_MediaId",
                table: "StreamingLinks",
                column: "MediaId");

            // The anime stored so far have no links. Marking them as never synchronized makes the next visit to each
            // fetch its data again (replacing it with the same fields, plus the links) instead of waiting for the
            // seven days of the cache. Nothing is deleted.
            migrationBuilder.Sql("UPDATE `Media` SET `LastSyncedAt` = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StreamingLinks");
        }
    }
}
