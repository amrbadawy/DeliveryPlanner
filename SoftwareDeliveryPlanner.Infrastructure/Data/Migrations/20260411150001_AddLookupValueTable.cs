using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupValueTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: new[] { "Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder" },
                columnTypes: new[] { "int", "nvarchar(50)", "nvarchar(50)", "nvarchar(100)", "bit", "int" },
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
                    { 16, "ResourceRole", "Developer", "Developer", true, 1 },
                    { 17, "ResourceRole", "Senior Developer", "Senior Developer", true, 2 },
                    { 18, "ResourceRole", "Tech Lead", "Tech Lead", true, 3 },
                    { 19, "ResourceRole", "QA", "QA", true, 4 },
                    { 20, "WorkingWeek", "sun_thu", "Sunday - Thursday", true, 1 },
                    { 21, "WorkingWeek", "mon_fri", "Monday - Friday", true, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_Category",
                table: "LookupValues",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_Category_Code",
                table: "LookupValues",
                columns: new[] { "Category", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LookupValues");
        }
    }
}
