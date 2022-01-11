using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class fixedPVPNumberQuestion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PvpRounds_Questions_NumberQuestionId",
                table: "PvpRounds");

            migrationBuilder.DropIndex(
                name: "IX_PvpRounds_NumberQuestionId",
                table: "PvpRounds");

            migrationBuilder.DropColumn(
                name: "NumberQuestionId",
                table: "PvpRounds");

            migrationBuilder.AddColumn<int>(
                name: "PvpRoundId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_PvpRoundId",
                table: "Questions",
                column: "PvpRoundId",
                unique: true,
                filter: "[PvpRoundId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_PvpRounds_PvpRoundId",
                table: "Questions",
                column: "PvpRoundId",
                principalTable: "PvpRounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_PvpRounds_PvpRoundId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_PvpRoundId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "PvpRoundId",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "NumberQuestionId",
                table: "PvpRounds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PvpRounds_NumberQuestionId",
                table: "PvpRounds",
                column: "NumberQuestionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PvpRounds_Questions_NumberQuestionId",
                table: "PvpRounds",
                column: "NumberQuestionId",
                principalTable: "Questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
