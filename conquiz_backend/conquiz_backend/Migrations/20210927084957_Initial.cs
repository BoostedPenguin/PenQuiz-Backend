using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace conquiz_backend.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameResult",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    description = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameResult", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    question = table.Column<string>(maxLength: 255, nullable: true),
                    isNumberQuestion = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    totalRounds = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(maxLength: 100, nullable: false),
                    username = table.Column<string>(maxLength: 50, nullable: false),
                    isAdmin = table.Column<bool>(nullable: false),
                    isBanned = table.Column<bool>(nullable: false),
                    isOnline = table.Column<bool>(nullable: false),
                    provider = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "MapTerritory",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    territoryName = table.Column<string>(unicode: false, maxLength: 50, nullable: false),
                    mapId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapTerritory", x => x.id);
                    table.ForeignKey(
                        name: "FK_MapTerritory_Maps",
                        column: x => x.mapId,
                        principalTable: "Maps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    questionId = table.Column<int>(nullable: true),
                    answer = table.Column<string>(maxLength: 255, nullable: true),
                    correct = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.id);
                    table.ForeignKey(
                        name: "FK__Answers__questio__5DCAEF64",
                        column: x => x.questionId,
                        principalTable: "Questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsersId = table.Column<int>(nullable: false),
                    Token = table.Column<string>(nullable: true),
                    Expires = table.Column<DateTime>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    CreatedByIp = table.Column<string>(nullable: true),
                    Revoked = table.Column<DateTime>(nullable: true),
                    RevokedByIp = table.Column<string>(nullable: true),
                    ReplacedByToken = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => new { x.UsersId, x.Id });
                    table.ForeignKey(
                        name: "FK_RefreshToken_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Borders",
                columns: table => new
                {
                    ThisTerritory = table.Column<int>(nullable: false),
                    NextToTerritory = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_myConstraint", x => new { x.ThisTerritory, x.NextToTerritory });
                    table.ForeignKey(
                        name: "FK_Borders_MapTerritory1",
                        column: x => x.NextToTerritory,
                        principalTable: "MapTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Borders_MapTerritory",
                        column: x => x.ThisTerritory,
                        principalTable: "MapTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectTerritory",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mapTerritoryId = table.Column<int>(nullable: true),
                    mapObjectId = table.Column<int>(nullable: true),
                    takenBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectTerritory", x => x.id);
                    table.ForeignKey(
                        name: "FK__ObjectTer__mapTe__59FA5E80",
                        column: x => x.mapTerritoryId,
                        principalTable: "MapTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    playerId = table.Column<int>(nullable: true),
                    gameId = table.Column<int>(nullable: true),
                    score = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.id);
                    table.ForeignKey(
                        name: "FK__Participa__playe__571DF1D5",
                        column: x => x.playerId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameInstance",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    resultId = table.Column<int>(nullable: true),
                    questionTimerSeconds = table.Column<int>(nullable: true),
                    inProgress = table.Column<bool>(nullable: true),
                    start_time = table.Column<DateTime>(type: "datetime", nullable: true),
                    end_time = table.Column<DateTime>(type: "datetime", nullable: true),
                    Mapid = table.Column<int>(nullable: true),
                    ParticipantsId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameInstance", x => x.id);
                    table.ForeignKey(
                        name: "FK_GameInstance_Maps",
                        column: x => x.Mapid,
                        principalTable: "Maps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameInstance_Participants_ParticipantsId",
                        column: x => x.ParticipantsId,
                        principalTable: "Participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK__GameInsta__resul__59063A47",
                        column: x => x.resultId,
                        principalTable: "GameResult",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoundsHistory",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roundID = table.Column<int>(nullable: true),
                    gameInstanceId = table.Column<int>(nullable: true),
                    description = table.Column<string>(maxLength: 255, nullable: true),
                    attackerId = table.Column<int>(nullable: true),
                    defenderId = table.Column<int>(nullable: true),
                    roundWinnerId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundsHistory", x => x.id);
                    table.ForeignKey(
                        name: "FK__RoundsHis__gameI__5CD6CB2B",
                        column: x => x.gameInstanceId,
                        principalTable: "GameInstance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK__RoundsHis__round__5BE2A6F2",
                        column: x => x.roundID,
                        principalTable: "Rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoundQuestion",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roundId = table.Column<int>(nullable: true),
                    questionId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundQuestion", x => x.id);
                    table.ForeignKey(
                        name: "FK__RoundQues__quest__5FB337D6",
                        column: x => x.questionId,
                        principalTable: "Questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK__RoundQues__round__5EBF139D",
                        column: x => x.roundId,
                        principalTable: "RoundsHistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_questionId",
                table: "Answers",
                column: "questionId");

            migrationBuilder.CreateIndex(
                name: "IX_Borders_NextToTerritory",
                table: "Borders",
                column: "NextToTerritory");

            migrationBuilder.CreateIndex(
                name: "IX_GameInstance_Mapid",
                table: "GameInstance",
                column: "Mapid");

            migrationBuilder.CreateIndex(
                name: "IX_GameInstance_ParticipantsId",
                table: "GameInstance",
                column: "ParticipantsId");

            migrationBuilder.CreateIndex(
                name: "IX_GameInstance_resultId",
                table: "GameInstance",
                column: "resultId");

            migrationBuilder.CreateIndex(
                name: "IX_MapTerritory_mapId",
                table: "MapTerritory",
                column: "mapId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTerritory_mapObjectId",
                table: "ObjectTerritory",
                column: "mapObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTerritory_mapTerritoryId",
                table: "ObjectTerritory",
                column: "mapTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_gameId",
                table: "Participants",
                column: "gameId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_playerId",
                table: "Participants",
                column: "playerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundQuestion_questionId",
                table: "RoundQuestion",
                column: "questionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundQuestion_roundId",
                table: "RoundQuestion",
                column: "roundId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundsHistory_gameInstanceId",
                table: "RoundsHistory",
                column: "gameInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundsHistory_roundID",
                table: "RoundsHistory",
                column: "roundID");

            migrationBuilder.AddForeignKey(
                name: "FK__ObjectTer__mapOb__5AEE82B9",
                table: "ObjectTerritory",
                column: "mapObjectId",
                principalTable: "GameInstance",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__Participa__gameI__5812160E",
                table: "Participants",
                column: "gameId",
                principalTable: "GameInstance",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameInstance_Maps",
                table: "GameInstance");

            migrationBuilder.DropForeignKey(
                name: "FK_GameInstance_Participants_ParticipantsId",
                table: "GameInstance");

            migrationBuilder.DropTable(
                name: "Answers");

            migrationBuilder.DropTable(
                name: "Borders");

            migrationBuilder.DropTable(
                name: "ObjectTerritory");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "RoundQuestion");

            migrationBuilder.DropTable(
                name: "MapTerritory");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "RoundsHistory");

            migrationBuilder.DropTable(
                name: "Rounds");

            migrationBuilder.DropTable(
                name: "Maps");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "GameInstance");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "GameResult");
        }
    }
}
