using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class addGUIDexternalID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "externalId",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "userGlobalIdentifier",
                table: "Users",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "userGlobalIdentifier",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "externalId",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
