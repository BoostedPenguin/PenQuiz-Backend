using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class questionOpenedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "QuestionOpenedAt",
                table: "Round",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionOpenedAt",
                table: "Round");
        }
    }
}
