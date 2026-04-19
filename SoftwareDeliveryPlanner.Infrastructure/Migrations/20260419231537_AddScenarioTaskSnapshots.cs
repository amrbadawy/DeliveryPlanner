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

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 16,
                column: "Code",
                value: "DEV");

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Code", "DisplayName" },
                values: new object[] { "QA", "Quality Assurance" });

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Code", "DisplayName" },
                values: new object[] { "SA", "System Analyst" });

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Code", "DisplayName" },
                values: new object[] { "BA", "Business Analyst" });

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Category", "Code", "DisplayName", "SortOrder" },
                values: new object[] { "ResourceRole", "UX", "UX Designer", 5 });

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Category", "Code", "DisplayName", "SortOrder" },
                values: new object[] { "ResourceRole", "UI", "UI Designer", 6 });

            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: new[] { "Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
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

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 23);

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

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 16,
                column: "Code",
                value: "Developer");

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Code", "DisplayName" },
                values: new object[] { "Senior Developer", "Senior Developer" });

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Code", "DisplayName" },
                values: new object[] { "Tech Lead", "Tech Lead" });

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Code", "DisplayName" },
                values: new object[] { "QA", "QA" });

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Category", "Code", "DisplayName", "SortOrder" },
                values: new object[] { "WorkingWeek", "sun_thu", "Sunday - Thursday", 1 });

            migrationBuilder.UpdateData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Category", "Code", "DisplayName", "SortOrder" },
                values: new object[] { "WorkingWeek", "mon_fri", "Monday - Friday", 2 });

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
        }
    }
}
