using Microsoft.EntityFrameworkCore.Migrations;

namespace QuestionService.NpgsqlMigrations.Migrations
{
    public partial class externalid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "externalId",
                table: "GameInstances");

            migrationBuilder.AddColumn<string>(
                name: "externalGlobalId",
                table: "GameInstances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "externalGlobalId",
                table: "GameInstances");

            migrationBuilder.AddColumn<int>(
                name: "externalId",
                table: "GameInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
