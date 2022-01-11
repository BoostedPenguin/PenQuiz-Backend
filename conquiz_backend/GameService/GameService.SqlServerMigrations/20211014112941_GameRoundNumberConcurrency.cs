using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class GameRoundNumberConcurrency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "gameRoundNumber",
                table: "Rounds",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "isLastUntakenTerritories",
                table: "Rounds",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "gameRoundNumber",
                table: "GameInstance",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gameRoundNumber",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "isLastUntakenTerritories",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "gameRoundNumber",
                table: "GameInstance");
        }
    }
}
