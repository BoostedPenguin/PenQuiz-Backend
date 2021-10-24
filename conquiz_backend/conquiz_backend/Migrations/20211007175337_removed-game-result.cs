using Microsoft.EntityFrameworkCore.Migrations;

namespace conquiz_backend.Migrations
{
    public partial class removedgameresult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__GameInsta__resul__59063A47",
                table: "GameInstance");

            migrationBuilder.DropTable(
                name: "GameResult");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameResult",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameResult", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK__GameInsta__resul__59063A47",
                table: "GameInstance",
                column: "resultId",
                principalTable: "GameResult",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
