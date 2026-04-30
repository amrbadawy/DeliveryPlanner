using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanScenarioZoomLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GanttZoomLevel",
                schema: "planning",
                table: "PlanScenarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GanttZoomLevel",
                schema: "planning",
                table: "PlanScenarios");
        }
    }
}
