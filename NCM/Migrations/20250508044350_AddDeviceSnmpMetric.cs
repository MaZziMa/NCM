using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NCM.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceSnmpMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceSnmpMetrics",
                columns: table => new
                {
                    MetricId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetricValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceSnmpMetrics", x => x.MetricId);
                    table.ForeignKey(
                        name: "FK_DeviceSnmpMetrics_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSnmpMetrics_DeviceId",
                table: "DeviceSnmpMetrics",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceSnmpMetrics");
        }
    }
}
