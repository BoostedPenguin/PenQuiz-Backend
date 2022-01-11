using Microsoft.EntityFrameworkCore.Migrations;

namespace QuestionService.SqlServerMigrations.Migrations
{
    public partial class updatedmaintables2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameState",
                table: "GameInstances");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameState",
                table: "GameInstances",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
