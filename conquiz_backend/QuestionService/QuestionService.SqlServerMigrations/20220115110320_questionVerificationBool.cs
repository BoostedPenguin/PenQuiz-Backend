using Microsoft.EntityFrameworkCore.Migrations;

namespace QuestionService.SqlServerMigrations.Migrations
{
    public partial class questionVerificationBool : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "externalId",
                table: "GameInstances",
                newName: "externalGlobalId");

            migrationBuilder.AddColumn<bool>(
                name: "isVerified",
                table: "Questions",
                type: "bit",
                nullable: true,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isVerified",
                table: "Questions");

            migrationBuilder.RenameColumn(
                name: "externalGlobalId",
                table: "GameInstances",
                newName: "externalId");
        }
    }
}
