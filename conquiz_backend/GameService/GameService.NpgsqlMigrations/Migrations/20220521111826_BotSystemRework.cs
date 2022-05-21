using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class BotSystemRework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isBot",
                table: "Participants",
                newName: "isAfk");

            migrationBuilder.AddColumn<bool>(
                name: "isBot",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isBot",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "isAfk",
                table: "Participants",
                newName: "isBot");
        }
    }
}
