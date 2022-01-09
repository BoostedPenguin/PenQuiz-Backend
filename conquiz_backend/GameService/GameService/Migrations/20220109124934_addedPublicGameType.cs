using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class addedPublicGameType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameType",
                table: "GameInstance",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "PRIVATE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameType",
                table: "GameInstance");
        }
    }
}
