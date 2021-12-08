using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class capitalRoundIsCompletedCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isCompleted",
                table: "CapitalRound",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isCompleted",
                table: "CapitalRound");
        }
    }
}
