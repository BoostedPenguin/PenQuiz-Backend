using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class pvproundCapitalCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentlyCapitalStage",
                table: "PvpRounds",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsQuestionVotingOpen",
                table: "CapitalRound",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuestionOpenedAt",
                table: "CapitalRound",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCurrentlyCapitalStage",
                table: "PvpRounds");

            migrationBuilder.DropColumn(
                name: "IsQuestionVotingOpen",
                table: "CapitalRound");

            migrationBuilder.DropColumn(
                name: "QuestionOpenedAt",
                table: "CapitalRound");
        }
    }
}
