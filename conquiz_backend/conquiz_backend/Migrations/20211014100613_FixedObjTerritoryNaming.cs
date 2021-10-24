using Microsoft.EntityFrameworkCore.Migrations;

namespace conquiz_backend.Migrations
{
    public partial class FixedObjTerritoryNaming : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__ObjectTer__mapOb__5AEE82B9",
                table: "ObjectTerritory");

            migrationBuilder.DropIndex(
                name: "IX_ObjectTerritory_mapObjectId",
                table: "ObjectTerritory");

            migrationBuilder.DropColumn(
                name: "mapObjectId",
                table: "ObjectTerritory");

            migrationBuilder.AddColumn<int>(
                name: "gameInstanceId",
                table: "ObjectTerritory",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTerritory_gameInstanceId",
                table: "ObjectTerritory",
                column: "gameInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK__ObjectTer__gameIn__5AEE82B9",
                table: "ObjectTerritory",
                column: "gameInstanceId",
                principalTable: "GameInstance",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__ObjectTer__gameIn__5AEE82B9",
                table: "ObjectTerritory");

            migrationBuilder.DropIndex(
                name: "IX_ObjectTerritory_gameInstanceId",
                table: "ObjectTerritory");

            migrationBuilder.DropColumn(
                name: "gameInstanceId",
                table: "ObjectTerritory");

            migrationBuilder.AddColumn<int>(
                name: "mapObjectId",
                table: "ObjectTerritory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTerritory_mapObjectId",
                table: "ObjectTerritory",
                column: "mapObjectId");

            migrationBuilder.AddForeignKey(
                name: "FK__ObjectTer__mapOb__5AEE82B9",
                table: "ObjectTerritory",
                column: "mapObjectId",
                principalTable: "GameInstance",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
