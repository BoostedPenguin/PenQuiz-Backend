using Microsoft.EntityFrameworkCore.Migrations;

namespace net_core_backend.Migrations
{
    public partial class participantBotProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isBot",
                table: "Participants",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isBot",
                table: "Participants");
        }
    }
}
