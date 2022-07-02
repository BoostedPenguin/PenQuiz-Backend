using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class CharacterUsedByUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterUsers",
                columns: table => new
                {
                    BelongToUsersId = table.Column<int>(type: "integer", nullable: false),
                    OwnedCharactersId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterUsers", x => new { x.BelongToUsersId, x.OwnedCharactersId });
                    table.ForeignKey(
                        name: "FK_CharacterUsers_Characters_OwnedCharactersId",
                        column: x => x.OwnedCharactersId,
                        principalTable: "Characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterUsers_Users_BelongToUsersId",
                        column: x => x.BelongToUsersId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterUsers_OwnedCharactersId",
                table: "CharacterUsers",
                column: "OwnedCharactersId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterUsers");
        }
    }
}
