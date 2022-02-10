using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AccountService.NpgsqlMigrations.Migrations
{
    public partial class addedbandate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "bannedDate",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bannedDate",
                table: "Users");
        }
    }
}
