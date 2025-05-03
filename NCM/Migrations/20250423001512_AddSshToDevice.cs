using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NCM.Migrations
{
    /// <inheritdoc />
    public partial class AddSshToDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SshPassword",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SshUsername",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SshPassword",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "SshUsername",
                table: "Devices");
        }
    }
}
