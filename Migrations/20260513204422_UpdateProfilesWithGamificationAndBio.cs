using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenZCoders.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProfilesWithGamificationAndBio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "XpTotal",
                table: "StudentProfiles",
                newName: "TotalXp");

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "StudentProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "StaffProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "StaffProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfiles_AspNetUsers_UserId",
                table: "StudentProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfiles_AspNetUsers_UserId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "TotalXp",
                table: "StudentProfiles",
                newName: "XpTotal");
        }
    }
}
