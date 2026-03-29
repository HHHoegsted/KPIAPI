using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KPIAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDashboardConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RobotDashboardConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RobotDashboardConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RobotId = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FilterKpiKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FilterKpiTextEquals = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    HitlItemsAggregation = table.Column<int>(type: "integer", nullable: false),
                    HitlItemsKpiKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TotalItemsAggregation = table.Column<int>(type: "integer", nullable: false),
                    TotalItemsKpiKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotDashboardConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotDashboardConfigs_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RobotDashboardConfigs_RobotId",
                table: "RobotDashboardConfigs",
                column: "RobotId",
                unique: true);
        }
    }
}
