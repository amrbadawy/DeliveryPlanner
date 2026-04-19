using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDefaultRoles : Migration
    {
        /// <summary>
        /// No-op: all seed data changes were applied in AddScenarioTaskSnapshots migration.
        /// This migration exists only to keep the model snapshot in sync.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
