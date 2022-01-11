using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class territoryattackedbyid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isAttacked",
                table: "ObjectTerritory");

            migrationBuilder.AddColumn<int>(
                name: "attackedBy",
                table: "ObjectTerritory",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attackedBy",
                table: "ObjectTerritory");

            migrationBuilder.AddColumn<bool>(
                name: "isAttacked",
                table: "ObjectTerritory",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
