using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.SqlServerMigrations.Migrations
{
    public partial class nullablequestionroundid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_RoundId",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "RoundId",
                table: "Questions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_RoundId",
                table: "Questions",
                column: "RoundId",
                unique: true,
                filter: "[RoundId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_RoundId",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "RoundId",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_RoundId",
                table: "Questions",
                column: "RoundId",
                unique: true);
        }
    }
}
