using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class fixedmissingconstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__attTer__neuAtt__8AJAWDSW",
                table: "AttackingNeutralTerritory");

            migrationBuilder.DropColumn(
                name: "NeutralRoundId",
                table: "Round");

            migrationBuilder.DropColumn(
                name: "PvpRoundId",
                table: "Round");

            migrationBuilder.CreateIndex(
                name: "IX_AttackingNeutralTerritory_attackedTerritoryId",
                table: "AttackingNeutralTerritory",
                column: "attackedTerritoryId");

            migrationBuilder.AddForeignKey(
                name: "FK__attTer__neuAtt__8AJAWDSW",
                table: "AttackingNeutralTerritory",
                column: "attackedTerritoryId",
                principalTable: "ObjectTerritory",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__attTer__neuAtt__8AJAWDSW",
                table: "AttackingNeutralTerritory");

            migrationBuilder.DropIndex(
                name: "IX_AttackingNeutralTerritory_attackedTerritoryId",
                table: "AttackingNeutralTerritory");

            migrationBuilder.AddColumn<int>(
                name: "NeutralRoundId",
                table: "Round",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PvpRoundId",
                table: "Round",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK__attTer__neuAtt__8AJAWDSW",
                table: "AttackingNeutralTerritory",
                column: "NeutralRoundId",
                principalTable: "ObjectTerritory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
