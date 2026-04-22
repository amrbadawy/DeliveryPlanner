using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "resource");

            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "planning");

            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.EnsureSchema(
                name: "task");

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
                name: "CalendarDays",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    CalendarDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DayName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: false),
                    IsHoliday = table.Column<bool>(type: "bit", nullable: false),
                    HolidayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BaseCapacity = table.Column<double>(type: "float", nullable: false),
                    AdjCapacity = table.Column<double>(type: "float", nullable: false),
                    EffectiveCapacity = table.Column<double>(type: "float", nullable: false),
                    ReservedCapacity = table.Column<double>(type: "float", nullable: false),
                    RemainingCapacity = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                schema: "resource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HolidayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HolidayType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupValues", x => x.Id);
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
                name: "Roles",
                schema: "resource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.UniqueConstraint("AK_Roles_Code", x => x.Code);
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
                name: "Settings",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                schema: "task",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MaxResource = table.Column<double>(type: "float", nullable: false),
                    StrictDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SchedulingRank = table.Column<int>(type: "int", nullable: true),
                    PeakConcurrency = table.Column<double>(type: "float", nullable: true),
                    AssignedResourceId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedFinish = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DeliveryRisk = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OverrideStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PreferredResourceIds = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                    table.UniqueConstraint("AK_TaskItems_TaskId", x => x.TaskId);
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

            migrationBuilder.CreateTable(
                name: "ScenarioTaskSnapshots",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanScenarioId = table.Column<int>(type: "int", nullable: false),
                    TaskId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SchedulingRank = table.Column<int>(type: "int", nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedFinish = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    StrictDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedResourceId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PeakConcurrency = table.Column<double>(type: "float", nullable: true),
                    MaxResource = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryRisk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DependsOnTaskIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioTaskSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioTaskSnapshots_PlanScenarios_PlanScenarioId",
                        column: x => x.PlanScenarioId,
                        principalSchema: "planning",
                        principalTable: "PlanScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                schema: "resource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResourceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Team = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AvailabilityPct = table.Column<double>(type: "float", nullable: false),
                    DailyCapacity = table.Column<double>(type: "float", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Active = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SeniorityLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Mid"),
                    WorkingWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.UniqueConstraint("AK_TeamMembers_ResourceId", x => x.ResourceId);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Roles_Role",
                        column: x => x.Role,
                        principalSchema: "resource",
                        principalTable: "Roles",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Allocations",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaskId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    CalendarDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SchedRank = table.Column<int>(type: "int", nullable: true),
                    HoursAllocated = table.Column<double>(type: "float", nullable: false),
                    CumulativeEffort = table.Column<double>(type: "float", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    ServiceStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Allocations_TaskItems_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "task",
                        principalTable: "TaskItems",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskDependencies",
                schema: "task",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PredecessorTaskId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    LagDays = table.Column<int>(type: "int", nullable: false),
                    OverlapPct = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_TaskItems_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "task",
                        principalTable: "TaskItems",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskEffortBreakdowns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EstimationDays = table.Column<double>(type: "float", nullable: false),
                    OverlapPct = table.Column<double>(type: "float", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MinSeniority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskEffortBreakdowns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskEffortBreakdowns_TaskItems_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "task",
                        principalTable: "TaskItems",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioEffortSnapshots",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScenarioTaskSnapshotId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EstimationDays = table.Column<double>(type: "float", nullable: false),
                    OverlapPct = table.Column<double>(type: "float", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioEffortSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioEffortSnapshots_ScenarioTaskSnapshots_ScenarioTaskSnapshotId",
                        column: x => x.ScenarioTaskSnapshotId,
                        principalSchema: "planning",
                        principalTable: "ScenarioTaskSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Adjustments",
                schema: "resource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdjStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdjEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailabilityPct = table.Column<double>(type: "float", nullable: false),
                    AdjType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adjustments_TeamMembers_ResourceId",
                        column: x => x.ResourceId,
                        principalSchema: "resource",
                        principalTable: "TeamMembers",
                        principalColumn: "ResourceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: new[] { "Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "TaskStatus", "Not Started", "Not Started", true, 1 },
                    { 2, "TaskStatus", "In Progress", "In Progress", true, 2 },
                    { 3, "TaskStatus", "Completed", "Completed", true, 3 },
                    { 4, "DeliveryRisk", "On Track", "On Track", true, 1 },
                    { 5, "DeliveryRisk", "At Risk", "At Risk", true, 2 },
                    { 6, "DeliveryRisk", "Late", "Late", true, 3 },
                    { 7, "HolidayType", "National", "National", true, 1 },
                    { 8, "HolidayType", "Religious", "Religious", true, 2 },
                    { 9, "HolidayType", "Company", "Company", true, 3 },
                    { 10, "AdjustmentType", "Vacation", "Vacation", true, 1 },
                    { 11, "AdjustmentType", "Training", "Training", true, 2 },
                    { 12, "AdjustmentType", "Sick Leave", "Sick Leave", true, 3 },
                    { 13, "AdjustmentType", "Other", "Other", true, 4 },
                    { 14, "ActiveStatus", "Yes", "Yes", true, 1 },
                    { 15, "ActiveStatus", "No", "No", true, 2 },
                    { 16, "ResourceRole", "DEV", "Developer", true, 1 },
                    { 17, "ResourceRole", "QA", "Quality Assurance", true, 2 },
                    { 18, "ResourceRole", "SA", "System Analyst", true, 3 },
                    { 19, "ResourceRole", "BA", "Business Analyst", true, 4 },
                    { 20, "ResourceRole", "UX", "UX Designer", true, 5 },
                    { 21, "ResourceRole", "UI", "UI Designer", true, 6 },
                    { 22, "WorkingWeek", "sun_thu", "Sunday - Thursday", true, 1 },
                    { 23, "WorkingWeek", "mon_fri", "Monday - Friday", true, 2 }
                });

            migrationBuilder.InsertData(
                schema: "resource",
                table: "Roles",
                columns: new[] { "Id", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "DEV", "Developer", true, 1 },
                    { 2, "QA", "Quality Assurance", true, 2 },
                    { 3, "SA", "System Analyst", true, 3 },
                    { 4, "BA", "Business Analyst", true, 4 },
                    { 5, "UX", "UX Designer", true, 5 },
                    { 6, "UI", "UI Designer", true, 6 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_ResourceId",
                schema: "resource",
                table: "Adjustments",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_TaskId",
                schema: "scheduling",
                table: "Allocations",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Timestamp",
                schema: "audit",
                table: "AuditEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDays_DateKey",
                schema: "scheduling",
                table: "CalendarDays",
                column: "DateKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_EndDate",
                schema: "resource",
                table: "Holidays",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_StartDate",
                schema: "resource",
                table: "Holidays",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_Category",
                table: "LookupValues",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_Category_Code",
                table: "LookupValues",
                columns: new[] { "Category", "Code" },
                unique: true);

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
                name: "IX_Roles_Code",
                schema: "resource",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioEffortSnapshots_ScenarioTaskSnapshotId",
                schema: "planning",
                table: "ScenarioEffortSnapshots",
                column: "ScenarioTaskSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioTaskSnapshots_PlanScenarioId",
                schema: "planning",
                table: "ScenarioTaskSnapshots",
                column: "PlanScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Key",
                schema: "scheduling",
                table: "Settings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_TaskId_PredecessorTaskId",
                schema: "task",
                table: "TaskDependencies",
                columns: new[] { "TaskId", "PredecessorTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskEffortBreakdowns_TaskId",
                table: "TaskEffortBreakdowns",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TaskId",
                schema: "task",
                table: "TaskItems",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskNotes_TaskId",
                schema: "task",
                table: "TaskNotes",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_ResourceId",
                schema: "resource",
                table: "TeamMembers",
                column: "ResourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_Role",
                schema: "resource",
                table: "TeamMembers",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adjustments",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "Allocations",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "AuditEntries",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "CalendarDays",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "Holidays",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "LookupValues");

            migrationBuilder.DropTable(
                name: "RiskNotifications",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "ScenarioEffortSnapshots",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "SchedulerSnapshots",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "Settings",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "TaskDependencies",
                schema: "task");

            migrationBuilder.DropTable(
                name: "TaskEffortBreakdowns");

            migrationBuilder.DropTable(
                name: "TaskNotes",
                schema: "task");

            migrationBuilder.DropTable(
                name: "TeamMembers",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "ScenarioTaskSnapshots",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "TaskItems",
                schema: "task");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "PlanScenarios",
                schema: "planning");
        }
    }
}
