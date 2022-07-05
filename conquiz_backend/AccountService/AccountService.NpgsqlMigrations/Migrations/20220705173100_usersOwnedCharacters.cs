using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace AccountService.NpgsqlMigrations.Migrations
{
    public partial class usersOwnedCharacters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterGlobalIdentifier = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    PricingType = table.Column<int>(type: "integer", nullable: false),
                    CharacterType = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

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
                        principalColumn: "Id",
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

            migrationBuilder.DropTable(
                name: "Characters");
        }
    }
}
