using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class roundsterritoryrelation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attackedBy",
                table: "ObjectTerritory");

            migrationBuilder.AddColumn<int>(
                name: "attackingTerritoryId",
                table: "Rounds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "answeredAt",
                table: "RoundAnswers",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_attackingTerritoryId",
                table: "Rounds",
                column: "attackingTerritoryId");

            migrationBuilder.AddForeignKey(
                name: "FK__RoundsHis__objTerr__AWDJIK3S",
                table: "Rounds",
                column: "attackingTerritoryId",
                principalTable: "ObjectTerritory",
                principalColumn: "id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__RoundsHis__objTerr__AWDJIK3S",
                table: "Rounds");

            migrationBuilder.DropIndex(
                name: "IX_Rounds_attackingTerritoryId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "attackingTerritoryId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "answeredAt",
                table: "RoundAnswers");

            migrationBuilder.AddColumn<int>(
                name: "attackedBy",
                table: "ObjectTerritory",
                type: "int",
                nullable: true);
        }
    }
}
