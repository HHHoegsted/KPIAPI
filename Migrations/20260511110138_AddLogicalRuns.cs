using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KPIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLogicalRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogicalRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RobotId = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogicalRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogicalRuns_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogicalRunAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LogicalRunId = table.Column<int>(type: "integer", nullable: false),
                    RobotRunId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    AddedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogicalRunAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogicalRunAttempts_LogicalRuns_LogicalRunId",
                        column: x => x.LogicalRunId,
                        principalTable: "LogicalRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogicalRunAttempts_RobotRuns_RobotRunId",
                        column: x => x.RobotRunId,
                        principalTable: "RobotRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogicalRunAttempts_LogicalRunId_SortOrder",
                table: "LogicalRunAttempts",
                columns: new[] { "LogicalRunId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LogicalRunAttempts_RobotRunId",
                table: "LogicalRunAttempts",
                column: "RobotRunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogicalRuns_RobotId_CreatedUtc",
                table: "LogicalRuns",
                columns: new[] { "RobotId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogicalRunAttempts");

            migrationBuilder.DropTable(
                name: "LogicalRuns");
        }
    }
}
