using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class gametypefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "GameType",
                table: "GameInstance",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "PRIVATE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GameType",
                table: "GameInstance",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "PRIVATE",
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
