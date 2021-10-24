using Microsoft.EntityFrameworkCore.Migrations;

namespace conquiz_backend.Migrations
{
    public partial class winneriddefaultnull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "roundWinnerId",
                table: "Rounds",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "roundWinnerId",
                table: "Rounds",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);
        }
    }
}
