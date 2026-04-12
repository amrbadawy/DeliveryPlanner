using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaskId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    CalendarDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SchedRank = table.Column<int>(type: "int", nullable: true),
                    MaxDev = table.Column<double>(type: "float", nullable: true),
                    AvailableCapacity = table.Column<double>(type: "float", nullable: true),
                    AssignedDev = table.Column<double>(type: "float", nullable: false),
                    CumulativeEffort = table.Column<double>(type: "float", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    ServiceStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Allocations_TaskItems_TaskId",
                        column: x => x.TaskId,
                        principalTable: "TaskItems",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_TaskId",
                table: "Allocations",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Allocations");
        }
    }
}
