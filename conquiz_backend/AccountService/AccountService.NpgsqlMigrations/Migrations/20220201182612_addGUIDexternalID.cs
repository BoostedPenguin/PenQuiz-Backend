using Microsoft.EntityFrameworkCore.Migrations;

namespace AccountService.NpgsqlMigrations.Migrations
{
    public partial class addGUIDexternalID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "userGlobalIdentifier",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "userGlobalIdentifier",
                table: "Users");
        }
    }
}
