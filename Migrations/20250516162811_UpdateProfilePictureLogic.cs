using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace junimo_v3.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProfilePictureLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "profile_picture_url",
                table: "User",
                newName: "profile_picture_content_type");

            migrationBuilder.AddColumn<byte[]>(
                name: "profile_picture",
                table: "User",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_picture",
                table: "User");

            migrationBuilder.RenameColumn(
                name: "profile_picture_content_type",
                table: "User",
                newName: "profile_picture_url");
        }
    }
}
