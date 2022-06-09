using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class CharacterModelsAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatarName",
                table: "Participants");

            migrationBuilder.AddColumn<int>(
                name: "inGameParticipantNumber",
                table: "Participants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    characterGlobalIdentifier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    avatarName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "penguinAvatar.svg"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    abilityDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    pricingType = table.Column<string>(type: "text", nullable: false),
                    characterType = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "GameCharacters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipantId = table.Column<int>(type: "integer", nullable: false),
                    CharacterId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameCharacters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameCharacters_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameCharacters_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameCharacterAbilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterType = table.Column<int>(type: "integer", nullable: false),
                    GameCharacterId = table.Column<int>(type: "integer", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    CurrentBonusPoints = table.Column<double>(type: "double precision", nullable: true),
                    FortifyCapitalUseCount = table.Column<int>(type: "integer", nullable: true),
                    MCQuestionHintUseCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameCharacterAbilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameCharacterAbilities_GameCharacters_GameCharacterId",
                        column: x => x.GameCharacterId,
                        principalTable: "GameCharacters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameCharacterAbilities_GameCharacterId",
                table: "GameCharacterAbilities",
                column: "GameCharacterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameCharacters_CharacterId",
                table: "GameCharacters",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_GameCharacters_ParticipantId",
                table: "GameCharacters",
                column: "ParticipantId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameCharacterAbilities");

            migrationBuilder.DropTable(
                name: "GameCharacters");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropColumn(
                name: "inGameParticipantNumber",
                table: "Participants");

            migrationBuilder.AddColumn<string>(
                name: "avatarName",
                table: "Participants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "penguinAvatar.svg");
        }
    }
}
