using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoleSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "SortOrder",
                value: 5);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortOrder",
                value: 6);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "SortOrder",
                value: 2);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "SortOrder",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "SortOrder",
                value: 3);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6,
                column: "SortOrder",
                value: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "SortOrder",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortOrder",
                value: 2);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "SortOrder",
                value: 3);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "SortOrder",
                value: 4);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "SortOrder",
                value: 5);

            migrationBuilder.UpdateData(
                schema: "resource",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6,
                column: "SortOrder",
                value: 6);
        }
    }
}
