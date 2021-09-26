using Microsoft.EntityFrameworkCore.Migrations;

namespace net_core_backend.Migrations
{
    public partial class removedredunanttable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapObject");

            migrationBuilder.AddColumn<int>(
                name: "Mapid",
                table: "GameInstance",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParticipantsId",
                table: "GameInstance",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameInstance_ParticipantsId",
                table: "GameInstance",
                column: "ParticipantsId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameInstance_Participants_ParticipantsId",
                table: "GameInstance",
                column: "ParticipantsId",
                principalTable: "Participants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameInstance_Participants_ParticipantsId",
                table: "GameInstance");

            migrationBuilder.DropIndex(
                name: "IX_GameInstance_ParticipantsId",
                table: "GameInstance");

            migrationBuilder.DropColumn(
                name: "Mapid",
                table: "GameInstance");

            migrationBuilder.DropColumn(
                name: "ParticipantsId",
                table: "GameInstance");

            migrationBuilder.CreateTable(
                name: "MapObject",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    gameInstanceId = table.Column<int>(type: "int", nullable: true),
                    mapid = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapObject", x => x.id);
                });
        }
    }
}
