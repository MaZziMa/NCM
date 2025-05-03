using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NCM.Migrations
{
    /// <inheritdoc />
    public partial class AddLastBackupTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastBackupTime",
                table: "Devices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastBackupTime",
                table: "Devices");
        }
    }
}
