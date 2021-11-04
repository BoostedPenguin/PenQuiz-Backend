using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class removedquestionstable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoundQuestion");

            migrationBuilder.AddColumn<int>(
                name: "RoundsId",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_RoundsId",
                table: "Questions",
                column: "RoundsId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Rounds_RoundsId",
                table: "Questions",
                column: "RoundsId",
                principalTable: "Rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Rounds_RoundsId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_RoundsId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "RoundsId",
                table: "Questions");

            migrationBuilder.CreateTable(
                name: "RoundQuestion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    questionId = table.Column<int>(type: "int", nullable: false),
                    roundId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundQuestion", x => x.id);
                    table.ForeignKey(
                        name: "FK__RoundQues__quest__5FB337D6",
                        column: x => x.questionId,
                        principalTable: "Questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__RoundQues__round__5EBF139D",
                        column: x => x.roundId,
                        principalTable: "Rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoundQuestion_questionId",
                table: "RoundQuestion",
                column: "questionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundQuestion_roundId",
                table: "RoundQuestion",
                column: "roundId");
        }
    }
}
