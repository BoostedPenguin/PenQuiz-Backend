using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class removeduselessroundstage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoundStage",
                table: "Round");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoundStage",
                table: "Round",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "NOT_STARTED");
        }
    }
}
