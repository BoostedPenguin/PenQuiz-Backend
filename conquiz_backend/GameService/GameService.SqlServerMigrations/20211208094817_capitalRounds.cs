using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class capitalRounds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CapitalRoundId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CapitalRound",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PvpRoundId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapitalRound", x => x.id);
                    table.ForeignKey(
                        name: "FK__capitalRou__pvpRound__JAWD2",
                        column: x => x.PvpRoundId,
                        principalTable: "PvpRounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CapitalRoundAnswers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    mChoiceQAnswerId = table.Column<int>(type: "int", nullable: true),
                    numberQAnswer = table.Column<long>(type: "bigint", nullable: true),
                    numebrQAnsweredAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CapitalRoundId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapitalRoundAnswers", x => x.id);
                    table.ForeignKey(
                        name: "FK__capitalRouAns__capitalRou__KOAWD",
                        column: x => x.CapitalRoundId,
                        principalTable: "CapitalRound",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CapitalRoundId",
                table: "Questions",
                column: "CapitalRoundId",
                unique: true,
                filter: "[CapitalRoundId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalRound_PvpRoundId",
                table: "CapitalRound",
                column: "PvpRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalRoundAnswers_CapitalRoundId",
                table: "CapitalRoundAnswers",
                column: "CapitalRoundId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_CapitalRound_CapitalRoundId",
                table: "Questions",
                column: "CapitalRoundId",
                principalTable: "CapitalRound",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_CapitalRound_CapitalRoundId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "CapitalRoundAnswers");

            migrationBuilder.DropTable(
                name: "CapitalRound");

            migrationBuilder.DropIndex(
                name: "IX_Questions_CapitalRoundId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CapitalRoundId",
                table: "Questions");
        }
    }
}
