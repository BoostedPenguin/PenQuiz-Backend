using Microsoft.EntityFrameworkCore.Migrations;

namespace net_core_backend.Migrations
{
    public partial class relationsmigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RoundsHistory_gameInstanceId",
                table: "RoundsHistory",
                column: "gameInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundsHistory_roundID",
                table: "RoundsHistory",
                column: "roundID");

            migrationBuilder.CreateIndex(
                name: "IX_RoundQuestion_questionId",
                table: "RoundQuestion",
                column: "questionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundQuestion_roundId",
                table: "RoundQuestion",
                column: "roundId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_gameId",
                table: "Participants",
                column: "gameId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_playerId",
                table: "Participants",
                column: "playerId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTerritory_mapObjectId",
                table: "ObjectTerritory",
                column: "mapObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTerritory_mapTerritoryId",
                table: "ObjectTerritory",
                column: "mapTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_GameInstance_Mapid",
                table: "GameInstance",
                column: "Mapid");

            migrationBuilder.CreateIndex(
                name: "IX_GameInstance_resultId",
                table: "GameInstance",
                column: "resultId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_questionId",
                table: "Answers",
                column: "questionId");

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
                name: "FK__Participa__gameI__5812160E",
                table: "Participants",
                column: "gameId",
                principalTable: "GameInstance",
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Answers__questio__5DCAEF64",
                table: "Answers");

            migrationBuilder.DropForeignKey(
                name: "FK_GameInstance_Maps",
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
                name: "FK__Participa__gameI__5812160E",
                table: "Participants");

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

            migrationBuilder.DropIndex(
                name: "IX_RoundsHistory_gameInstanceId",
                table: "RoundsHistory");

            migrationBuilder.DropIndex(
                name: "IX_RoundsHistory_roundID",
                table: "RoundsHistory");

            migrationBuilder.DropIndex(
                name: "IX_RoundQuestion_questionId",
                table: "RoundQuestion");

            migrationBuilder.DropIndex(
                name: "IX_RoundQuestion_roundId",
                table: "RoundQuestion");

            migrationBuilder.DropIndex(
                name: "IX_Participants_gameId",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_Participants_playerId",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_ObjectTerritory_mapObjectId",
                table: "ObjectTerritory");

            migrationBuilder.DropIndex(
                name: "IX_ObjectTerritory_mapTerritoryId",
                table: "ObjectTerritory");

            migrationBuilder.DropIndex(
                name: "IX_GameInstance_Mapid",
                table: "GameInstance");

            migrationBuilder.DropIndex(
                name: "IX_GameInstance_resultId",
                table: "GameInstance");

            migrationBuilder.DropIndex(
                name: "IX_Answers_questionId",
                table: "Answers");
        }
    }
}
