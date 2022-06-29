using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameService.NpgsqlMigrations.Migrations
{
    public partial class UserOwnedCharacters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UsersId",
                table: "Characters",
                column: "UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Users_UsersId",
                table: "Characters",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Users_UsersId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_UsersId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "Characters");
        }
    }
}
