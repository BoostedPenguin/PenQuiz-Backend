using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace QuestionService.NpgsqlMigrations.Migrations
{
    public partial class questionSubmitted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "submittedAt",
                table: "Questions",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "submittedByUsername",
                table: "Questions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verifiedAt",
                table: "Questions",
                type: "timestamp without time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "submittedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "submittedByUsername",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "verifiedAt",
                table: "Questions");
        }
    }
}
