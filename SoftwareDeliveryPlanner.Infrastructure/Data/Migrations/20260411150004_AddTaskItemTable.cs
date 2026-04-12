using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DevEstimation = table.Column<double>(type: "float", nullable: false),
                    MaxDev = table.Column<double>(type: "float", nullable: false),
                    StrictDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SchedulingRank = table.Column<int>(type: "int", nullable: true),
                    AssignedDev = table.Column<double>(type: "float", nullable: true),
                    AssignedResourceId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedFinish = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DeliveryRisk = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OverrideStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OverrideDev = table.Column<double>(type: "float", nullable: true),
                    DependsOnTaskIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                    table.UniqueConstraint("AK_TaskItems_TaskId", x => x.TaskId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TaskId",
                table: "TaskItems",
                column: "TaskId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TaskItems");
        }
    }
}
