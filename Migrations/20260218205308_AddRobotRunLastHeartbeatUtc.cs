using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KPIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRobotRunLastHeartbeatUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastHeartbeatUtc",
                table: "RobotRuns",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastHeartbeatUtc",
                table: "RobotRuns");
        }
    }
}
