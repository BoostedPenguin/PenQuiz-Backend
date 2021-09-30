using Microsoft.EntityFrameworkCore.Migrations;

namespace net_core_backend.Migrations
{
    public partial class fixedgameinstanceparticipants : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameInstance_Participants_ParticipantsId",
                table: "GameInstance");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_GameInstance_Participants_ParticipantsId",
                table: "GameInstance",
                column: "ParticipantsId",
                principalTable: "Participants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
