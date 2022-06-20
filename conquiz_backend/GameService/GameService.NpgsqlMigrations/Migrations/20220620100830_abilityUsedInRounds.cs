using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class abilityUsedInRounds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<int>>(
                name: "AbilityUsedInRounds",
                table: "GameCharacterAbilities",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<List<int>>(
                name: "WizardCharacterAbilities_AbilityUsedInRounds",
                table: "GameCharacterAbilities",
                type: "integer[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbilityUsedInRounds",
                table: "GameCharacterAbilities");

            migrationBuilder.DropColumn(
                name: "WizardCharacterAbilities_AbilityUsedInRounds",
                table: "GameCharacterAbilities");
        }
    }
}
