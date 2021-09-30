﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace net_core_backend.Migrations
{
    public partial class usergamelock : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isInGame",
                table: "Users",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isInGame",
                table: "Users");
        }
    }
}
