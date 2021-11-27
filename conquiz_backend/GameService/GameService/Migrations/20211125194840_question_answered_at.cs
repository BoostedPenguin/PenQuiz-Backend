using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class question_answered_at : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "answered_at",
                table: "AttackingNeutralTerritory",
                type: "datetime",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "answered_at",
                table: "AttackingNeutralTerritory");
        }
    }
}
