using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class reworkedroundstable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__RoundQues__round__5EBF139D",
                table: "RoundQuestion");

            migrationBuilder.DropTable(
                name: "RoundsHistory");

            migrationBuilder.DropColumn(
                name: "totalRounds",
                table: "Rounds");

            migrationBuilder.AddColumn<int>(
                name: "attackerId",
                table: "Rounds",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "defenderId",
                table: "Rounds",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "Rounds",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "gameInstanceId",
                table: "Rounds",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RoundStage",
                table: "Rounds",
                nullable: false,
                defaultValue: "NOT_STARTED");

            migrationBuilder.AddColumn<int>(
                name: "roundWinnerId",
                table: "Rounds",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_gameInstanceId",
                table: "Rounds",
                column: "gameInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK__RoundQues__round__5EBF139D",
                table: "RoundQuestion",
                column: "roundId",
                principalTable: "Rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__RoundsHis__gameI__5CD6CB2B",
                table: "Rounds",
                column: "gameInstanceId",
                principalTable: "GameInstance",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__RoundQues__round__5EBF139D",
                table: "RoundQuestion");

            migrationBuilder.DropForeignKey(
                name: "FK__RoundsHis__gameI__5CD6CB2B",
                table: "Rounds");

            migrationBuilder.DropIndex(
                name: "IX_Rounds_gameInstanceId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "attackerId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "defenderId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "description",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "gameInstanceId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "RoundStage",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "roundWinnerId",
                table: "Rounds");

            migrationBuilder.AddColumn<int>(
                name: "totalRounds",
                table: "Rounds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RoundsHistory",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attackerId = table.Column<int>(type: "int", nullable: false),
                    defenderId = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    gameInstanceId = table.Column<int>(type: "int", nullable: false),
                    roundID = table.Column<int>(type: "int", nullable: false),
                    roundWinnerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundsHistory", x => x.id);
                    table.ForeignKey(
                        name: "FK__RoundsHis__gameI__5CD6CB2B",
                        column: x => x.gameInstanceId,
                        principalTable: "GameInstance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__RoundsHis__round__5BE2A6F2",
                        column: x => x.roundID,
                        principalTable: "Rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoundsHistory_gameInstanceId",
                table: "RoundsHistory",
                column: "gameInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundsHistory_roundID",
                table: "RoundsHistory",
                column: "roundID");

            migrationBuilder.AddForeignKey(
                name: "FK__RoundQues__round__5EBF139D",
                table: "RoundQuestion",
                column: "roundId",
                principalTable: "RoundsHistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
