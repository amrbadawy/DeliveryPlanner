using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScenarioTaskSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Migrate existing resource data: old role codes → new codes ──
            // Guarded with IF EXISTS so migration is safe on fresh databases
            migrationBuilder.Sql(@"
                IF OBJECT_ID('[resource].[TeamMembers]', 'U') IS NOT NULL
                BEGIN
                    UPDATE [resource].[TeamMembers] SET [Role] = 'DEV' WHERE [Role] = 'Developer';
                    UPDATE [resource].[TeamMembers] SET [Role] = 'DEV' WHERE [Role] = 'Senior Developer';
                    UPDATE [resource].[TeamMembers] SET [Role] = 'DEV' WHERE [Role] = 'Tech Lead';
                END
            ");

            // ── Remove old Roles seed data ──
            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4);

            // ── Remove old LookupValues: ResourceRole (ids 16-19) + WorkingWeek (ids 20-21) ──
            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 21);

            // ── Create ScenarioTaskSnapshots table ──
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
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedFinish = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    StrictDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedResourceId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AssignedDev = table.Column<double>(type: "float", nullable: true),
                    DevEstimation = table.Column<double>(type: "float", nullable: false),
                    MaxDev = table.Column<double>(type: "float", nullable: false),
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

            // ── Insert new ResourceRole LookupValues ──
            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: new[] { "Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 16, "ResourceRole", "DEV", "Developer", true, 1 },
                    { 17, "ResourceRole", "QA", "Quality Assurance", true, 2 },
                    { 18, "ResourceRole", "SA", "System Analyst", true, 3 },
                    { 19, "ResourceRole", "BA", "Business Analyst", true, 4 },
                    { 20, "ResourceRole", "UX", "UX Designer", true, 5 },
                    { 21, "ResourceRole", "UI", "UI Designer", true, 6 }
                });

            // ── Insert new WorkingWeek LookupValues ──
            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: new[] { "Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 22, "WorkingWeek", "sun_thu", "Sunday - Thursday", true, 1 },
                    { 23, "WorkingWeek", "mon_fri", "Monday - Friday", true, 2 }
                });

            // ── Insert new Roles seed data ──
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
                name: "IX_ScenarioTaskSnapshots_PlanScenarioId",
                schema: "planning",
                table: "ScenarioTaskSnapshots",
                column: "PlanScenarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScenarioTaskSnapshots",
                schema: "planning");

            // ── Remove new LookupValues ──
            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 23);

            // ── Remove new Roles ──
            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6);

            // ── Restore original LookupValues ──
            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: new[] { "Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 16, "ResourceRole", "Developer", "Developer", true, 1 },
                    { 17, "ResourceRole", "Senior Developer", "Senior Developer", true, 2 },
                    { 18, "ResourceRole", "Tech Lead", "Tech Lead", true, 3 },
                    { 19, "ResourceRole", "QA", "QA", true, 4 },
                    { 20, "WorkingWeek", "sun_thu", "Sunday - Thursday", true, 1 },
                    { 21, "WorkingWeek", "mon_fri", "Monday - Friday", true, 2 }
                });

            // ── Restore original Roles ──
            migrationBuilder.InsertData(
                schema: "resource",
                table: "Roles",
                columns: new[] { "Id", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Developer", "Developer", true, 1 },
                    { 2, "Senior Developer", "Senior Developer", true, 2 },
                    { 3, "Tech Lead", "Tech Lead", true, 3 },
                    { 4, "QA", "QA", true, 4 }
                });

            // ── Revert resource data ──
            migrationBuilder.Sql(@"
                IF OBJECT_ID('[resource].[TeamMembers]', 'U') IS NOT NULL
                BEGIN
                    UPDATE [resource].[TeamMembers] SET [Role] = 'Developer' WHERE [Role] = 'DEV';
                END
            ");
        }
    }
}
