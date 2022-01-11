using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class initialNPGSQL : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    isInGame = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    externalId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "GameInstance",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    resultId = table.Column<int>(type: "integer", nullable: false),
                    questionTimerSeconds = table.Column<int>(type: "integer", nullable: false),
                    GameType = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Mapid = table.Column<int>(type: "integer", nullable: false),
                    ParticipantsId = table.Column<int>(type: "integer", nullable: false),
                    gameCreatorId = table.Column<int>(type: "integer", nullable: false),
                    GameState = table.Column<string>(type: "text", nullable: false, defaultValue: "IN_LOBBY"),
                    invitationLink = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    gameRoundNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameInstance", x => x.id);
                    table.ForeignKey(
                        name: "FK_GameInstance_Maps",
                        column: x => x.Mapid,
                        principalTable: "Maps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapTerritory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    territoryName = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    mapId = table.Column<int>(type: "integer", nullable: false)
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
                name: "Participants",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    avatarName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "penguinAvatar.svg"),
                    playerId = table.Column<int>(type: "integer", nullable: false),
                    gameId = table.Column<int>(type: "integer", nullable: false),
                    isBot = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    FinalQuestionScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.id);
                    table.ForeignKey(
                        name: "FK__Participa__gameI__5812160E",
                        column: x => x.gameId,
                        principalTable: "GameInstance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK__Participa__playe__571DF1D5",
                        column: x => x.playerId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Round",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttackStage = table.Column<string>(type: "text", nullable: false, defaultValue: "MULTIPLE_NEUTRAL"),
                    gameInstanceId = table.Column<int>(type: "integer", nullable: false),
                    gameRoundNumber = table.Column<int>(type: "integer", nullable: false),
                    isTerritoryVotingOpen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    isQuestionVotingOpen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    QuestionOpenedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Round", x => x.id);
                    table.ForeignKey(
                        name: "FK__Round__gameI__5CD6CB2B",
                        column: x => x.gameInstanceId,
                        principalTable: "GameInstance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Borders",
                columns: table => new
                {
                    ThisTerritory = table.Column<int>(type: "integer", nullable: false),
                    NextToTerritory = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_myConstraint", x => new { x.ThisTerritory, x.NextToTerritory });
                    table.ForeignKey(
                        name: "FK_Borders_MapTerritory",
                        column: x => x.ThisTerritory,
                        principalTable: "MapTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Borders_MapTerritory1",
                        column: x => x.NextToTerritory,
                        principalTable: "MapTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObjectTerritory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mapTerritoryId = table.Column<int>(type: "integer", nullable: false),
                    gameInstanceId = table.Column<int>(type: "integer", nullable: false),
                    isCapital = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    territoryScore = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    takenBy = table.Column<int>(type: "integer", nullable: true),
                    attackedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectTerritory", x => x.id);
                    table.ForeignKey(
                        name: "FK__ObjectTer__gameIn__5AEE82B9",
                        column: x => x.gameInstanceId,
                        principalTable: "GameInstance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__ObjectTer__mapTe__59FA5E80",
                        column: x => x.mapTerritoryId,
                        principalTable: "MapTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NeutralRound",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoundId = table.Column<int>(type: "integer", nullable: false),
                    attackOrderNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeutralRound", x => x.id);
                    table.ForeignKey(
                        name: "FK_NeutralRound_Round_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Round",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PvpRounds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attackerId = table.Column<int>(type: "integer", nullable: false),
                    defenderId = table.Column<int>(type: "integer", nullable: true),
                    winnerId = table.Column<int>(type: "integer", nullable: true),
                    attackedTerritoryId = table.Column<int>(type: "integer", nullable: true),
                    RoundId = table.Column<int>(type: "integer", nullable: false),
                    IsCurrentlyCapitalStage = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PvpRounds", x => x.id);
                    table.ForeignKey(
                        name: "FK_PvpRounds_ObjectTerritory_attackedTerritoryId",
                        column: x => x.attackedTerritoryId,
                        principalTable: "ObjectTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PvpRounds_Round_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Round",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttackingNeutralTerritory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attackOrderNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    attackedTerritoryId = table.Column<int>(type: "integer", nullable: true),
                    answered_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    attackerWon = table.Column<bool>(type: "boolean", nullable: true),
                    attackerId = table.Column<int>(type: "integer", nullable: false),
                    NeutralRoundId = table.Column<int>(type: "integer", nullable: false),
                    attackerMChoiceQAnswerId = table.Column<int>(type: "integer", nullable: true),
                    attackerNumberQAnswer = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttackingNeutralTerritory", x => x.id);
                    table.ForeignKey(
                        name: "FK__attTer__neuAtt__8AJAWDSW",
                        column: x => x.attackedTerritoryId,
                        principalTable: "ObjectTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK__NeuRound__terAtt__8AWDJXCS",
                        column: x => x.NeutralRoundId,
                        principalTable: "NeutralRound",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CapitalRound",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CapitalRoundAttackStage = table.Column<int>(type: "integer", nullable: false),
                    isCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PvpRoundId = table.Column<int>(type: "integer", nullable: false),
                    IsQuestionVotingOpen = table.Column<bool>(type: "boolean", nullable: false),
                    QuestionOpenedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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
                name: "PvpRoundAnswers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<int>(type: "integer", nullable: false),
                    mChoiceQAnswerId = table.Column<int>(type: "integer", nullable: true),
                    numberQAnswer = table.Column<long>(type: "bigint", nullable: true),
                    NumberQAnsweredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PvpRoundId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PvpRoundAnswers", x => x.id);
                    table.ForeignKey(
                        name: "FK__pvpRou__pvpRouAns__A8AWDJBNS",
                        column: x => x.PvpRoundId,
                        principalTable: "PvpRounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CapitalRoundAnswers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<int>(type: "integer", nullable: false),
                    mChoiceQAnswerId = table.Column<int>(type: "integer", nullable: true),
                    numberQAnswer = table.Column<long>(type: "bigint", nullable: true),
                    numebrQAnsweredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CapitalRoundId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapitalRoundAnswers", x => x.id);
                    table.ForeignKey(
                        name: "FK_CapitalRoundAnswers_CapitalRound_CapitalRoundId",
                        column: x => x.CapitalRoundId,
                        principalTable: "CapitalRound",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    question = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RoundId = table.Column<int>(type: "integer", nullable: true),
                    PvpRoundId = table.Column<int>(type: "integer", nullable: true),
                    CapitalRoundMCId = table.Column<int>(type: "integer", nullable: true),
                    CapitalRoundNumberId = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_Questions_CapitalRound_CapitalRoundMCId",
                        column: x => x.CapitalRoundMCId,
                        principalTable: "CapitalRound",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_CapitalRound_CapitalRoundNumberId",
                        column: x => x.CapitalRoundNumberId,
                        principalTable: "CapitalRound",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_PvpRounds_PvpRoundId",
                        column: x => x.PvpRoundId,
                        principalTable: "PvpRounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_Round_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Round",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: true),
                    Correct = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Answers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttackingNeutralTerritory_attackedTerritoryId",
                table: "AttackingNeutralTerritory",
                column: "attackedTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AttackingNeutralTerritory_NeutralRoundId",
                table: "AttackingNeutralTerritory",
                column: "NeutralRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Borders_NextToTerritory",
                table: "Borders",
                column: "NextToTerritory");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalRound_PvpRoundId",
                table: "CapitalRound",
                column: "PvpRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalRoundAnswers_CapitalRoundId",
                table: "CapitalRoundAnswers",
                column: "CapitalRoundId");

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
                name: "IX_NeutralRound_RoundId",
                table: "NeutralRound",
                column: "RoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTerritory_gameInstanceId",
                table: "ObjectTerritory",
                column: "gameInstanceId");

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
                name: "IX_PvpRoundAnswers_PvpRoundId",
                table: "PvpRoundAnswers",
                column: "PvpRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_PvpRounds_attackedTerritoryId",
                table: "PvpRounds",
                column: "attackedTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PvpRounds_RoundId",
                table: "PvpRounds",
                column: "RoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CapitalRoundMCId",
                table: "Questions",
                column: "CapitalRoundMCId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CapitalRoundNumberId",
                table: "Questions",
                column: "CapitalRoundNumberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_PvpRoundId",
                table: "Questions",
                column: "PvpRoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_RoundId",
                table: "Questions",
                column: "RoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Round_gameInstanceId",
                table: "Round",
                column: "gameInstanceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Answers");

            migrationBuilder.DropTable(
                name: "AttackingNeutralTerritory");

            migrationBuilder.DropTable(
                name: "Borders");

            migrationBuilder.DropTable(
                name: "CapitalRoundAnswers");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "PvpRoundAnswers");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "NeutralRound");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "CapitalRound");

            migrationBuilder.DropTable(
                name: "PvpRounds");

            migrationBuilder.DropTable(
                name: "ObjectTerritory");

            migrationBuilder.DropTable(
                name: "Round");

            migrationBuilder.DropTable(
                name: "MapTerritory");

            migrationBuilder.DropTable(
                name: "GameInstance");

            migrationBuilder.DropTable(
                name: "Maps");
        }
    }
}
