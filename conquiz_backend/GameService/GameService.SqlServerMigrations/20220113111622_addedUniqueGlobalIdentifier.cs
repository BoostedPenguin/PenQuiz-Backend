using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class addedUniqueGlobalIdentifier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Answers__questio__5DCAEF64",
                table: "Answers");

            migrationBuilder.RenameColumn(
                name: "questionId",
                table: "Answers",
                newName: "QuestionId");

            migrationBuilder.RenameColumn(
                name: "correct",
                table: "Answers",
                newName: "Correct");

            migrationBuilder.RenameColumn(
                name: "answer",
                table: "Answers",
                newName: "Answer");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Answers",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Answers_questionId",
                table: "Answers",
                newName: "IX_Answers_QuestionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_time",
                table: "GameInstance",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_time",
                table: "GameInstance",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gameGlobalIdentifier",
                table: "GameInstance",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "numebrQAnsweredAt",
                table: "CapitalRoundAnswers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "answered_at",
                table: "AttackingNeutralTerritory",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Answer",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_Questions_QuestionId",
                table: "Answers",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_Questions_QuestionId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "gameGlobalIdentifier",
                table: "GameInstance");

            migrationBuilder.RenameColumn(
                name: "QuestionId",
                table: "Answers",
                newName: "questionId");

            migrationBuilder.RenameColumn(
                name: "Correct",
                table: "Answers",
                newName: "correct");

            migrationBuilder.RenameColumn(
                name: "Answer",
                table: "Answers",
                newName: "answer");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Answers",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                newName: "IX_Answers_questionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_time",
                table: "GameInstance",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_time",
                table: "GameInstance",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "numebrQAnsweredAt",
                table: "CapitalRoundAnswers",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "answered_at",
                table: "AttackingNeutralTerritory",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "answer",
                table: "Answers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK__Answers__questio__5DCAEF64",
                table: "Answers",
                column: "questionId",
                principalTable: "Questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
