using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class bothTypeCapitalQuestions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_CapitalRound_CapitalRoundId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_CapitalRoundId",
                table: "Questions");

            migrationBuilder.RenameColumn(
                name: "CapitalRoundId",
                table: "Questions",
                newName: "CapitalRoundNumberId");

            migrationBuilder.AddColumn<int>(
                name: "CapitalRoundMCId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CapitalRoundAttackStage",
                table: "CapitalRound",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CapitalRoundMCId",
                table: "Questions",
                column: "CapitalRoundMCId",
                unique: true,
                filter: "[CapitalRoundMCId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CapitalRoundNumberId",
                table: "Questions",
                column: "CapitalRoundNumberId",
                unique: true,
                filter: "[CapitalRoundNumberId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_CapitalRound_CapitalRoundMCId",
                table: "Questions",
                column: "CapitalRoundMCId",
                principalTable: "CapitalRound",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_CapitalRound_CapitalRoundNumberId",
                table: "Questions",
                column: "CapitalRoundNumberId",
                principalTable: "CapitalRound",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_CapitalRound_CapitalRoundMCId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_CapitalRound_CapitalRoundNumberId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_CapitalRoundMCId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_CapitalRoundNumberId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CapitalRoundMCId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CapitalRoundAttackStage",
                table: "CapitalRound");

            migrationBuilder.RenameColumn(
                name: "CapitalRoundNumberId",
                table: "Questions",
                newName: "CapitalRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CapitalRoundId",
                table: "Questions",
                column: "CapitalRoundId",
                unique: true,
                filter: "[CapitalRoundId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_CapitalRound_CapitalRoundId",
                table: "Questions",
                column: "CapitalRoundId",
                principalTable: "CapitalRound",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
