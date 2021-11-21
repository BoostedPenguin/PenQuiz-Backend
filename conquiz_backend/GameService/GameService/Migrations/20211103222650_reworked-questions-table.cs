using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class reworkedquestionstable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isNumberQuestion",
                table: "Questions");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "Questions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "Questions");

            migrationBuilder.AddColumn<bool>(
                name: "isNumberQuestion",
                table: "Questions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
