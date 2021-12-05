using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class pvpRoundTerritoryIdFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__attTer__pvpRou__123JKAWD",
                table: "PvpRounds");

            migrationBuilder.RenameColumn(
                name: "AttackedTerritoryId",
                table: "PvpRounds",
                newName: "attackedTerritoryId");

            migrationBuilder.RenameIndex(
                name: "IX_PvpRounds_AttackedTerritoryId",
                table: "PvpRounds",
                newName: "IX_PvpRounds_attackedTerritoryId");

            migrationBuilder.AlterColumn<int>(
                name: "attackedTerritoryId",
                table: "PvpRounds",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_PvpRounds_ObjectTerritory_attackedTerritoryId",
                table: "PvpRounds",
                column: "attackedTerritoryId",
                principalTable: "ObjectTerritory",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PvpRounds_ObjectTerritory_attackedTerritoryId",
                table: "PvpRounds");

            migrationBuilder.RenameColumn(
                name: "attackedTerritoryId",
                table: "PvpRounds",
                newName: "AttackedTerritoryId");

            migrationBuilder.RenameIndex(
                name: "IX_PvpRounds_attackedTerritoryId",
                table: "PvpRounds",
                newName: "IX_PvpRounds_AttackedTerritoryId");

            migrationBuilder.AlterColumn<int>(
                name: "AttackedTerritoryId",
                table: "PvpRounds",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK__attTer__pvpRou__123JKAWD",
                table: "PvpRounds",
                column: "AttackedTerritoryId",
                principalTable: "ObjectTerritory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
