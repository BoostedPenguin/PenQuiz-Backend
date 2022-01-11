using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class capitalFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__capitalRouAns__capitalRou__KOAWD",
                table: "CapitalRoundAnswers");

            migrationBuilder.AddForeignKey(
                name: "FK_CapitalRoundAnswers_CapitalRound_CapitalRoundId",
                table: "CapitalRoundAnswers",
                column: "CapitalRoundId",
                principalTable: "CapitalRound",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CapitalRoundAnswers_CapitalRound_CapitalRoundId",
                table: "CapitalRoundAnswers");

            migrationBuilder.AddForeignKey(
                name: "FK__capitalRouAns__capitalRou__KOAWD",
                table: "CapitalRoundAnswers",
                column: "CapitalRoundId",
                principalTable: "CapitalRound",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
