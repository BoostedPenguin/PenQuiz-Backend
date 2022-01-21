using Microsoft.EntityFrameworkCore.Migrations;

namespace QuestionService.NpgsqlMigrations.Migrations
{
    public partial class addedVerificationEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isVerified",
                table: "Questions");

            migrationBuilder.AddColumn<string>(
                name: "verificationStatus",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verificationStatus",
                table: "Questions");

            migrationBuilder.AddColumn<bool>(
                name: "isVerified",
                table: "Questions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
