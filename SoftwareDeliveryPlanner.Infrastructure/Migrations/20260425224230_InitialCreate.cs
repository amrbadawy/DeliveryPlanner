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
                name: "ActiveStatuses",
                schema: "resource",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveStatuses", x => x.Code);
                    table.CheckConstraint("CK_ActiveStatuses_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'");
                });

            migrationBuilder.CreateTable(
                name: "AdjustmentTypes",
                schema: "resource",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdjustmentTypes", x => x.Code);
                    table.CheckConstraint("CK_AdjustmentTypes_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'");
                });

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
                name: "DeliveryRisks",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRisks", x => x.Code);
                    table.CheckConstraint("CK_DeliveryRisks_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'");
                });

            migrationBuilder.CreateTable(
                name: "HolidayTypes",
                schema: "resource",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayTypes", x => x.Code);
                    table.CheckConstraint("CK_HolidayTypes_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'");
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
                    UnscheduledCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                name: "TaskStatuses",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStatuses", x => x.Code);
                    table.CheckConstraint("CK_TaskStatuses_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'");
                });

            migrationBuilder.CreateTable(
                name: "WorkingWeeks",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingWeeks", x => x.Code);
                    table.CheckConstraint("CK_WorkingWeeks_Code_Format", "Code = UPPER(Code) AND Code NOT LIKE '% %'");
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
                    HolidayType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holidays_HolidayTypes_HolidayType",
                        column: x => x.HolidayType,
                        principalSchema: "resource",
                        principalTable: "HolidayTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
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
                name: "TaskItems",
                schema: "task",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StrictDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SchedulingRank = table.Column<int>(type: "int", nullable: true),
                    PeakConcurrency = table.Column<double>(type: "float", nullable: true),
                    AssignedResourceId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedFinish = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryRisk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.ForeignKey(
                        name: "FK_TaskItems_DeliveryRisks_DeliveryRisk",
                        column: x => x.DeliveryRisk,
                        principalTable: "DeliveryRisks",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskItems_TaskStatuses_Status",
                        column: x => x.Status,
                        principalTable: "TaskStatuses",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
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
                    Active = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SeniorityLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Mid"),
                    WorkingWeek = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.UniqueConstraint("AK_TeamMembers_ResourceId", x => x.ResourceId);
                    table.ForeignKey(
                        name: "FK_TeamMembers_ActiveStatuses_Active",
                        column: x => x.Active,
                        principalSchema: "resource",
                        principalTable: "ActiveStatuses",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Roles_Role",
                        column: x => x.Role,
                        principalSchema: "resource",
                        principalTable: "Roles",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMembers_WorkingWeeks_WorkingWeek",
                        column: x => x.WorkingWeek,
                        principalTable: "WorkingWeeks",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
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
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MaxFte = table.Column<double>(type: "float", nullable: false, defaultValue: 1.0)
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
                    MaxFte = table.Column<double>(type: "float", nullable: false, defaultValue: 1.0),
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
                    AdjType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adjustments_AdjustmentTypes_AdjType",
                        column: x => x.AdjType,
                        principalSchema: "resource",
                        principalTable: "AdjustmentTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Adjustments_TeamMembers_ResourceId",
                        column: x => x.ResourceId,
                        principalSchema: "resource",
                        principalTable: "TeamMembers",
                        principalColumn: "ResourceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "resource",
                table: "ActiveStatuses",
                columns: new[] { "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { "NO", "No", true, 2 },
                    { "YES", "Yes", true, 1 }
                });

            migrationBuilder.InsertData(
                schema: "resource",
                table: "AdjustmentTypes",
                columns: new[] { "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { "OTHER", "Other", true, 4 },
                    { "SICK_LEAVE", "Sick Leave", true, 3 },
                    { "TRAINING", "Training", true, 2 },
                    { "VACATION", "Vacation", true, 1 }
                });

            migrationBuilder.InsertData(
                table: "DeliveryRisks",
                columns: new[] { "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { "AT_RISK", "At Risk", true, 2 },
                    { "LATE", "Late", true, 3 },
                    { "ON_TRACK", "On Track", true, 1 }
                });

            migrationBuilder.InsertData(
                schema: "resource",
                table: "HolidayTypes",
                columns: new[] { "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { "COMPANY", "Company", true, 3 },
                    { "NATIONAL", "National", true, 1 },
                    { "RELIGIOUS", "Religious", true, 2 }
                });

            migrationBuilder.InsertData(
                schema: "resource",
                table: "Roles",
                columns: new[] { "Id", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "DEV", "Developer", true, 5 },
                    { 2, "QA", "Quality Assurance", true, 6 },
                    { 3, "SA", "System Analyst", true, 2 },
                    { 4, "BA", "Business Analyst", true, 1 },
                    { 5, "UX", "UX Designer", true, 3 },
                    { 6, "UI", "UI Designer", true, 4 }
                });

            migrationBuilder.InsertData(
                table: "TaskStatuses",
                columns: new[] { "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { "COMPLETED", "Completed", true, 3 },
                    { "IN_PROGRESS", "In Progress", true, 2 },
                    { "NOT_STARTED", "Not Started", true, 1 }
                });

            migrationBuilder.InsertData(
                table: "WorkingWeeks",
                columns: new[] { "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { "MON_FRI", "Monday - Friday", true, 2 },
                    { "SUN_THU", "Sunday - Thursday", true, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveStatuses_SortOrder",
                schema: "resource",
                table: "ActiveStatuses",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_AdjType",
                schema: "resource",
                table: "Adjustments",
                column: "AdjType");

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_ResourceId",
                schema: "resource",
                table: "Adjustments",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AdjustmentTypes_SortOrder",
                schema: "resource",
                table: "AdjustmentTypes",
                column: "SortOrder");

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
                name: "IX_DeliveryRisks_SortOrder",
                table: "DeliveryRisks",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_EndDate",
                schema: "resource",
                table: "Holidays",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_HolidayType",
                schema: "resource",
                table: "Holidays",
                column: "HolidayType");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_StartDate",
                schema: "resource",
                table: "Holidays",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayTypes_SortOrder",
                schema: "resource",
                table: "HolidayTypes",
                column: "SortOrder");

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
                name: "IX_TaskItems_DeliveryRisk",
                schema: "task",
                table: "TaskItems",
                column: "DeliveryRisk");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_Status",
                schema: "task",
                table: "TaskItems",
                column: "Status");

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
                name: "IX_TaskStatuses_SortOrder",
                table: "TaskStatuses",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_Active",
                schema: "resource",
                table: "TeamMembers",
                column: "Active");

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

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_WorkingWeek",
                schema: "resource",
                table: "TeamMembers",
                column: "WorkingWeek");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingWeeks_SortOrder",
                table: "WorkingWeeks",
                column: "SortOrder");
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
                name: "AdjustmentTypes",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "TeamMembers",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "HolidayTypes",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "ScenarioTaskSnapshots",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "TaskItems",
                schema: "task");

            migrationBuilder.DropTable(
                name: "ActiveStatuses",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "WorkingWeeks");

            migrationBuilder.DropTable(
                name: "PlanScenarios",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "DeliveryRisks");

            migrationBuilder.DropTable(
                name: "TaskStatuses");
        }
    }
}
