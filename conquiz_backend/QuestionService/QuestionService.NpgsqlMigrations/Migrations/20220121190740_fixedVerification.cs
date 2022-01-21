using Microsoft.EntityFrameworkCore.Migrations;

namespace QuestionService.NpgsqlMigrations.Migrations
{
    public partial class fixedVerification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "verificationStatus",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "VERIFIED",
                oldClrType: typeof(string),
                oldType: "text");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "verificationStatus",
                table: "Questions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "VERIFIED");
        }
    }
}
