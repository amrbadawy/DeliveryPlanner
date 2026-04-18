using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWaveFeatureEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "planning");

            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanScenarios",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScenarioName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalTasks = table.Column<int>(type: "int", nullable: false),
                    OnTrackCount = table.Column<int>(type: "int", nullable: false),
                    AtRiskCount = table.Column<int>(type: "int", nullable: false),
                    LateCount = table.Column<int>(type: "int", nullable: false),
                    EarliestStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LatestFinish = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalEstimation = table.Column<double>(type: "float", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanScenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskNotifications",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreviousRisk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentRisk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchedulerSnapshots",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnTrackCount = table.Column<int>(type: "int", nullable: false),
                    AtRiskCount = table.Column<int>(type: "int", nullable: false),
                    LateCount = table.Column<int>(type: "int", nullable: false),
                    TotalTasks = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulerSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskNotes",
                schema: "task",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NoteText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskNotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Timestamp",
                schema: "audit",
                table: "AuditEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_RiskNotifications_IsRead",
                schema: "notification",
                table: "RiskNotifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_RiskNotifications_TaskId",
                schema: "notification",
                table: "RiskNotifications",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNotes_TaskId",
                schema: "task",
                table: "TaskNotes",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "PlanScenarios",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "RiskNotifications",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "SchedulerSnapshots",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "TaskNotes",
                schema: "task");
        }
    }
}
