using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NCM.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MustContain",
                table: "ComplianceRules");

            migrationBuilder.DropColumn(
                name: "MustNotContain",
                table: "ComplianceRules");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "DeviceConfigs",
                newName: "UploadTime");

            migrationBuilder.RenameColumn(
                name: "ConfigText",
                table: "DeviceConfigs",
                newName: "ConfigContent");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ComplianceRules",
                newName: "RuleName");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "SshUsername",
                table: "Devices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ComplianceRules",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequiredString",
                table: "ComplianceRules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ComplianceRules");

            migrationBuilder.DropColumn(
                name: "RequiredString",
                table: "ComplianceRules");

            migrationBuilder.RenameColumn(
                name: "UploadTime",
                table: "DeviceConfigs",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ConfigContent",
                table: "DeviceConfigs",
                newName: "ConfigText");

            migrationBuilder.RenameColumn(
                name: "RuleName",
                table: "ComplianceRules",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Devices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SshUsername",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "MustContain",
                table: "ComplianceRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MustNotContain",
                table: "ComplianceRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
