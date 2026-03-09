using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KPIAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Robots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CenterCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Robots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KpiDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RobotId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ValueType = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiDefinitions_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RobotDashboardConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RobotId = table.Column<int>(type: "integer", nullable: false),
                    TotalItemsKpiKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    HitlItemsKpiKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TotalItemsAggregation = table.Column<int>(type: "integer", nullable: false),
                    HitlItemsAggregation = table.Column<int>(type: "integer", nullable: false),
                    FilterKpiKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FilterKpiTextEquals = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "RobotRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RobotId = table.Column<int>(type: "integer", nullable: false),
                    RunId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<int>(type: "integer", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotRuns_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RobotRunId = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    CorrelationKey = table.Column<string>(type: "text", nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunEvents_RobotRuns_RobotRunId",
                        column: x => x.RobotRunId,
                        principalTable: "RobotRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KpiMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunEventId = table.Column<int>(type: "integer", nullable: false),
                    KpiDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    RecordedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IntValue = table.Column<long>(type: "bigint", nullable: true),
                    DecimalValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    BoolValue = table.Column<bool>(type: "boolean", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    TextValue = table.Column<string>(type: "text", nullable: true),
                    ValueType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiMeasurements_KpiDefinitions_KpiDefinitionId",
                        column: x => x.KpiDefinitionId,
                        principalTable: "KpiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiMeasurements_RunEvents_RunEventId",
                        column: x => x.RunEventId,
                        principalTable: "RunEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_RobotId_Key",
                table: "KpiDefinitions",
                columns: new[] { "RobotId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KpiMeasurements_KpiDefinitionId",
                table: "KpiMeasurements",
                column: "KpiDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiMeasurements_RunEventId_KpiDefinitionId",
                table: "KpiMeasurements",
                columns: new[] { "RunEventId", "KpiDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotDashboardConfigs_RobotId",
                table: "RobotDashboardConfigs",
                column: "RobotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RobotRuns_RobotId_RunId",
                table: "RobotRuns",
                columns: new[] { "RobotId", "RunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RobotRuns_RobotId_StartTimeUtc",
                table: "RobotRuns",
                columns: new[] { "RobotId", "StartTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Robots_Key",
                table: "Robots",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunEvents_RobotRunId",
                table: "RunEvents",
                column: "RobotRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KpiMeasurements");

            migrationBuilder.DropTable(
                name: "RobotDashboardConfigs");

            migrationBuilder.DropTable(
                name: "KpiDefinitions");

            migrationBuilder.DropTable(
                name: "RunEvents");

            migrationBuilder.DropTable(
                name: "RobotRuns");

            migrationBuilder.DropTable(
                name: "Robots");
        }
    }
}
