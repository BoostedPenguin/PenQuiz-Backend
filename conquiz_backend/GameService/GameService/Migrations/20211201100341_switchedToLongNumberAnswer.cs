using Microsoft.EntityFrameworkCore.Migrations;

namespace GameService.Migrations
{
    public partial class switchedToLongNumberAnswer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "attackerNumberQAnswer",
                table: "AttackingNeutralTerritory",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "attackerNumberQAnswer",
                table: "AttackingNeutralTerritory",
                type: "int",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
