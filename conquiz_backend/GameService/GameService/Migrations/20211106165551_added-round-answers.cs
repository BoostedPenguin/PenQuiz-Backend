using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class addedroundanswers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isVotingOpen",
                table: "Rounds",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RoundAnswers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    answerId = table.Column<int>(type: "int", nullable: false),
                    roundId = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_RoundAnswers_answerId",
                table: "RoundAnswers",
                column: "answerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundAnswers_roundId",
                table: "RoundAnswers",
                column: "roundId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoundAnswers");

            migrationBuilder.DropColumn(
                name: "isVotingOpen",
                table: "Rounds");
        }
    }
}
