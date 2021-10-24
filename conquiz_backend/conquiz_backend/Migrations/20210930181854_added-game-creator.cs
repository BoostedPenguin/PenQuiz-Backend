using Microsoft.EntityFrameworkCore.Migrations;

namespace conquiz_backend.Migrations
{
    public partial class addedgamecreator : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "gameCreatorId",
                table: "GameInstance",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gameCreatorId",
                table: "GameInstance");
        }
    }
}
