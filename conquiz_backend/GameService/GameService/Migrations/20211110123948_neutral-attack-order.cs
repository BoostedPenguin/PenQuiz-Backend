using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class neutralattackorder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attackOrderNumber",
                table: "NeutralRound",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "attackOrderNumber",
                table: "AttackingNeutralTerritory",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attackOrderNumber",
                table: "NeutralRound");

            migrationBuilder.DropColumn(
                name: "attackOrderNumber",
                table: "AttackingNeutralTerritory");
        }
    }
}
