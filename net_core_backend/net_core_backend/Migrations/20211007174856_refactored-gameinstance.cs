using Microsoft.EntityFrameworkCore.Migrations;

namespace net_core_backend.Migrations
{
    public partial class refactoredgameinstance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "inProgress",
                table: "GameInstance");

            migrationBuilder.AddColumn<string>(
                name: "GameState",
                table: "GameInstance",
                nullable: false,
                defaultValue: "IN_LOBBY");

            migrationBuilder.AddColumn<string>(
                name: "invitationLink",
                table: "GameInstance",
                maxLength: 1500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameState",
                table: "GameInstance");

            migrationBuilder.DropColumn(
                name: "invitationLink",
                table: "GameInstance");

            migrationBuilder.AddColumn<bool>(
                name: "inProgress",
                table: "GameInstance",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
