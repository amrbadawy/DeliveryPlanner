using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarDayTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarDays",
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

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDays_DateKey",
                table: "CalendarDays",
                column: "DateKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CalendarDays");
        }
    }
}
