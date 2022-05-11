using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class ParticipantInGameNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "inGameParticipantNumber",
                table: "Participants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "inGameParticipantNumber",
                table: "Participants");
        }
    }
}
