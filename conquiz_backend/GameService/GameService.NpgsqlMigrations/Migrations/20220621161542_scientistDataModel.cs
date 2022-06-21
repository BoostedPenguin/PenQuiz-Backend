using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class scientistDataModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberQuestionHintUseCount",
                table: "GameCharacterAbilities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<int>>(
                name: "VikingCharacterAbilities_AbilityUsedInRounds",
                table: "GameCharacterAbilities",
                type: "integer[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberQuestionHintUseCount",
                table: "GameCharacterAbilities");

            migrationBuilder.DropColumn(
                name: "VikingCharacterAbilities_AbilityUsedInRounds",
                table: "GameCharacterAbilities");
        }
    }
}
