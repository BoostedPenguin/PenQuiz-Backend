using Microsoft.EntityFrameworkCore.Migrations;

namespace conquiz_backend.Migrations
{
    public partial class attackedbyterritory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attackedBy",
                table: "ObjectTerritory",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attackedBy",
                table: "ObjectTerritory");
        }
    }
}
