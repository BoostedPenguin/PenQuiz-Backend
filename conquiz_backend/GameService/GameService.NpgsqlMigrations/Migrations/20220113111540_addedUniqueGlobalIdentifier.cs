using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class addedUniqueGlobalIdentifier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gameGlobalIdentifier",
                table: "GameInstance",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gameGlobalIdentifier",
                table: "GameInstance");
        }
    }
}
