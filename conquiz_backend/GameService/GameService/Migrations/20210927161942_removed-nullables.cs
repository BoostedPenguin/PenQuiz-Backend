using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class removednullables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Answers__questio__5DCAEF64",
                table: "Answers");

            migrationBuilder.DropForeignKey(
                name: "FK_GameInstance_Maps",
                table: "GameInstance");

            migrationBuilder.DropForeignKey(
                name: "FK_GameInstance_Participants_ParticipantsId",
                table: "GameInstance");

            migrationBuilder.DropForeignKey(
                name: "FK__GameInsta__resul__59063A47",
                table: "GameInstance");

            migrationBuilder.DropForeignKey(
                name: "FK__ObjectTer__mapOb__5AEE82B9",
                table: "ObjectTerritory");

            migrationBuilder.DropForeignKey(
                name: "FK__ObjectTer__mapTe__59FA5E80",
                table: "ObjectTerritory");

            migrationBuilder.DropForeignKey(
                name: "FK__Participa__playe__571DF1D5",
                table: "Participants");

            migrationBuilder.DropForeignKey(
                name: "FK__RoundQues__quest__5FB337D6",
                table: "RoundQuestion");

            migrationBuilder.DropForeignKey(
                name: "FK__RoundQues__round__5EBF139D",
                table: "RoundQuestion");

            migrationBuilder.DropForeignKey(
                name: "FK__RoundsHis__gameI__5CD6CB2B",
                table: "RoundsHistory");

            migrationBuilder.DropForeignKey(
                name: "FK__RoundsHis__round__5BE2A6F2",
                table: "RoundsHistory");

            migrationBuilder.AlterColumn<int>(
                name: "roundWinnerId",
                table: "RoundsHistory",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "roundID",
                table: "RoundsHistory",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "gameInstanceId",
                table: "RoundsHistory",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "defenderId",
                table: "RoundsHistory",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "attackerId",
                table: "RoundsHistory",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "totalRounds",
                table: "Rounds",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "roundId",
                table: "RoundQuestion",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "questionId",
                table: "RoundQuestion",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "isNumberQuestion",
                table: "Questions",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "score",
                table: "Participants",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "playerId",
                table: "Participants",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "gameId",
                table: "Participants",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "mapTerritoryId",
                table: "ObjectTerritory",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "mapObjectId",
                table: "ObjectTerritory",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_time",
                table: "GameInstance",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "resultId",
                table: "GameInstance",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "questionTimerSeconds",
                table: "GameInstance",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ParticipantsId",
                table: "GameInstance",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Mapid",
                table: "GameInstance",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "inProgress",
                table: "GameInstance",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "questionId",
                table: "Answers",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "correct",
                table: "Answers",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK__Answers__questio__5DCAEF64",
                table: "Answers",
                column: "questionId",
                principalTable: "Questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameInstance_Maps",
                table: "GameInstance",
                column: "Mapid",
                principalTable: "Maps",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameInstance_Participants_ParticipantsId",
                table: "GameInstance",
                column: "ParticipantsId",
                principalTable: "Participants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__GameInsta__resul__59063A47",
                table: "GameInstance",
                column: "resultId",
                principalTable: "GameResult",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__ObjectTer__mapOb__5AEE82B9",
                table: "ObjectTerritory",
                column: "mapObjectId",
                principalTable: "GameInstance",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__ObjectTer__mapTe__59FA5E80",
                table: "ObjectTerritory",
                column: "mapTerritoryId",
                principalTable: "MapTerritory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__Participa__playe__571DF1D5",
                table: "Participants",
                column: "playerId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__RoundQues__quest__5FB337D6",
                table: "RoundQuestion",
                column: "questionId",
                principalTable: "Questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__RoundQues__round__5EBF139D",
                table: "RoundQuestion",
                column: "roundId",
                principalTable: "RoundsHistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__RoundsHis__gameI__5CD6CB2B",
                table: "RoundsHistory",
                column: "gameInstanceId",
                principalTable: "GameInstance",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__RoundsHis__round__5BE2A6F2",
                table: "RoundsHistory",
                column: "roundID",
                principalTable: "Rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Answers__questio__5DCAEF64",
                table: "Answers");

            migrationBuilder.DropForeignKey(
                name: "FK_GameInstance_Maps",
                table: "GameInstance");

            migrationBuilder.DropForeignKey(
                name: "FK_GameInstance_Participants_ParticipantsId",
                table: "GameInstance");

            migrationBuilder.DropForeignKey(
                name: "FK__GameInsta__resul__59063A47",
                table: "GameInstance");

            migrationBuilder.DropForeignKey(
                name: "FK__ObjectTer__mapOb__5AEE82B9",
                table: "ObjectTerritory");

            migrationBuilder.DropForeignKey(
                name: "FK__ObjectTer__mapTe__59FA5E80",
                table: "ObjectTerritory");

            migrationBuilder.DropForeignKey(
                name: "FK__Participa__playe__571DF1D5",
                table: "Participants");

            migrationBuilder.DropForeignKey(
                name: "FK__RoundQues__quest__5FB337D6",
                table: "RoundQuestion");

            migrationBuilder.DropForeignKey(
                name: "FK__RoundQues__round__5EBF139D",
                table: "RoundQuestion");

            migrationBuilder.DropForeignKey(
                name: "FK__RoundsHis__gameI__5CD6CB2B",
                table: "RoundsHistory");

            migrationBuilder.DropForeignKey(
                name: "FK__RoundsHis__round__5BE2A6F2",
                table: "RoundsHistory");

            migrationBuilder.AlterColumn<int>(
                name: "roundWinnerId",
                table: "RoundsHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "roundID",
                table: "RoundsHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "gameInstanceId",
                table: "RoundsHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "defenderId",
                table: "RoundsHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "attackerId",
                table: "RoundsHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "totalRounds",
                table: "Rounds",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "roundId",
                table: "RoundQuestion",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "questionId",
                table: "RoundQuestion",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<bool>(
                name: "isNumberQuestion",
                table: "Questions",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool));

            migrationBuilder.AlterColumn<int>(
                name: "score",
                table: "Participants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "playerId",
                table: "Participants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "gameId",
                table: "Participants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "mapTerritoryId",
                table: "ObjectTerritory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "mapObjectId",
                table: "ObjectTerritory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_time",
                table: "GameInstance",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<int>(
                name: "resultId",
                table: "GameInstance",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "questionTimerSeconds",
                table: "GameInstance",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "ParticipantsId",
                table: "GameInstance",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<int>(
                name: "Mapid",
                table: "GameInstance",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<bool>(
                name: "inProgress",
                table: "GameInstance",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool));

            migrationBuilder.AlterColumn<int>(
                name: "questionId",
                table: "Answers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<bool>(
                name: "correct",
                table: "Answers",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool));

            migrationBuilder.AddForeignKey(
                name: "FK__Answers__questio__5DCAEF64",
                table: "Answers",
                column: "questionId",
                principalTable: "Questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GameInstance_Maps",
                table: "GameInstance",
                column: "Mapid",
                principalTable: "Maps",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GameInstance_Participants_ParticipantsId",
                table: "GameInstance",
                column: "ParticipantsId",
                principalTable: "Participants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__GameInsta__resul__59063A47",
                table: "GameInstance",
                column: "resultId",
                principalTable: "GameResult",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__ObjectTer__mapOb__5AEE82B9",
                table: "ObjectTerritory",
                column: "mapObjectId",
                principalTable: "GameInstance",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__ObjectTer__mapTe__59FA5E80",
                table: "ObjectTerritory",
                column: "mapTerritoryId",
                principalTable: "MapTerritory",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__Participa__playe__571DF1D5",
                table: "Participants",
                column: "playerId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__RoundQues__quest__5FB337D6",
                table: "RoundQuestion",
                column: "questionId",
                principalTable: "Questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__RoundQues__round__5EBF139D",
                table: "RoundQuestion",
                column: "roundId",
                principalTable: "RoundsHistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__RoundsHis__gameI__5CD6CB2B",
                table: "RoundsHistory",
                column: "gameInstanceId",
                principalTable: "GameInstance",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__RoundsHis__round__5BE2A6F2",
                table: "RoundsHistory",
                column: "roundID",
                principalTable: "Rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
