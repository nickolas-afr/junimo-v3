using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace junimo_v3.Migrations
{
    /// <inheritdoc />
    public partial class updateGameTable3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GameGenreV2",
                table: "GameGenreV2");

            migrationBuilder.AlterColumn<string>(
                name: "genre",
                table: "GameGenreV2",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameGenreV2",
                table: "GameGenreV2",
                columns: new[] { "game_id", "genre" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GameGenreV2",
                table: "GameGenreV2");

            migrationBuilder.AlterColumn<string>(
                name: "genre",
                table: "GameGenreV2",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameGenreV2",
                table: "GameGenreV2",
                column: "game_id");
        }
    }
}
