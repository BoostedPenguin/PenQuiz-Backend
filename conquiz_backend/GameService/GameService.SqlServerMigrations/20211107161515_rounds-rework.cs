using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class roundsrework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Rounds_RoundsId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "RoundAnswers");

            migrationBuilder.DropTable(
                name: "Rounds");

            migrationBuilder.DropIndex(
                name: "IX_Questions_RoundsId",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "RoundId",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "isAttacked",
                table: "ObjectTerritory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Round",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoundStage = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "NOT_STARTED"),
                    AttackStage = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "MULTIPLE_NEUTRAL"),
                    gameInstanceId = table.Column<int>(type: "int", nullable: false),
                    NeutralRoundId = table.Column<int>(type: "int", nullable: true),
                    PvpRoundId = table.Column<int>(type: "int", nullable: true),
                    gameRoundNumber = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    isTerritoryVotingOpen = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    isQuestionVotingOpen = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
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
                name: "NeutralRound",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoundId = table.Column<int>(type: "int", nullable: false)
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
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attackerId = table.Column<int>(type: "int", nullable: false),
                    defenderId = table.Column<int>(type: "int", nullable: true),
                    winnerId = table.Column<int>(type: "int", nullable: true),
                    AttackedTerritoryId = table.Column<int>(type: "int", nullable: false),
                    NumberQuestionId = table.Column<int>(type: "int", nullable: false),
                    RoundId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PvpRounds", x => x.id);
                    table.ForeignKey(
                        name: "FK__attTer__pvpRou__123JKAWD",
                        column: x => x.AttackedTerritoryId,
                        principalTable: "ObjectTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PvpRounds_Questions_NumberQuestionId",
                        column: x => x.NumberQuestionId,
                        principalTable: "Questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attackedTerritoryId = table.Column<int>(type: "int", nullable: true),
                    attackerWon = table.Column<bool>(type: "bit", nullable: true),
                    attackerId = table.Column<int>(type: "int", nullable: false),
                    NeutralRoundId = table.Column<int>(type: "int", nullable: false),
                    attackerMChoiceQAnswerId = table.Column<int>(type: "int", nullable: true),
                    attackerNumberQAnswer = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttackingNeutralTerritory", x => x.id);
                    table.ForeignKey(
                        name: "FK__attTer__neuAtt__8AJAWDSW",
                        column: x => x.NeutralRoundId,
                        principalTable: "ObjectTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__NeuRound__terAtt__8AWDJXCS",
                        column: x => x.NeutralRoundId,
                        principalTable: "NeutralRound",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PvpRoundAnswers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    mChoiceQAnswerId = table.Column<int>(type: "int", nullable: true),
                    numberQAnswer = table.Column<int>(type: "int", nullable: true),
                    PvpRoundId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Questions_RoundId",
                table: "Questions",
                column: "RoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttackingNeutralTerritory_NeutralRoundId",
                table: "AttackingNeutralTerritory",
                column: "NeutralRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_NeutralRound_RoundId",
                table: "NeutralRound",
                column: "RoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PvpRoundAnswers_PvpRoundId",
                table: "PvpRoundAnswers",
                column: "PvpRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_PvpRounds_AttackedTerritoryId",
                table: "PvpRounds",
                column: "AttackedTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PvpRounds_NumberQuestionId",
                table: "PvpRounds",
                column: "NumberQuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PvpRounds_RoundId",
                table: "PvpRounds",
                column: "RoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Round_gameInstanceId",
                table: "Round",
                column: "gameInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Round_RoundId",
                table: "Questions",
                column: "RoundId",
                principalTable: "Round",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Round_RoundId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "AttackingNeutralTerritory");

            migrationBuilder.DropTable(
                name: "PvpRoundAnswers");

            migrationBuilder.DropTable(
                name: "NeutralRound");

            migrationBuilder.DropTable(
                name: "PvpRounds");

            migrationBuilder.DropTable(
                name: "Round");

            migrationBuilder.DropIndex(
                name: "IX_Questions_RoundId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "RoundId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "isAttacked",
                table: "ObjectTerritory");

            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attackerId = table.Column<int>(type: "int", nullable: true),
                    attackingTerritoryId = table.Column<int>(type: "int", nullable: false),
                    defenderId = table.Column<int>(type: "int", nullable: true),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    gameInstanceId = table.Column<int>(type: "int", nullable: false),
                    gameRoundNumber = table.Column<int>(type: "int", nullable: false),
                    isLastUntakenTerritories = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    isVotingOpen = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RoundStage = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "NOT_STARTED"),
                    roundWinnerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.id);
                    table.ForeignKey(
                        name: "FK__RoundsHis__gameI__5CD6CB2B",
                        column: x => x.gameInstanceId,
                        principalTable: "GameInstance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__RoundsHis__objTerr__AWDJIK3S",
                        column: x => x.attackingTerritoryId,
                        principalTable: "ObjectTerritory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoundAnswers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    answerId = table.Column<int>(type: "int", nullable: false),
                    answeredAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    roundId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundAnswers", x => x.id);
                    table.ForeignKey(
                        name: "FK_RoundAn_Answ_123Xf8B",
                        column: x => x.roundId,
                        principalTable: "Answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundAn_round_912388B",
                        column: x => x.roundId,
                        principalTable: "Rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_RoundsId",
                table: "Questions",
                column: "RoundsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoundAnswers_answerId",
                table: "RoundAnswers",
                column: "answerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundAnswers_roundId",
                table: "RoundAnswers",
                column: "roundId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_attackingTerritoryId",
                table: "Rounds",
                column: "attackingTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_gameInstanceId",
                table: "Rounds",
                column: "gameInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Rounds_RoundsId",
                table: "Questions",
                column: "RoundsId",
                principalTable: "Rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
