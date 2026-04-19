using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDefaultRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Migrate existing resource data: old role codes → new codes ──
            migrationBuilder.Sql("UPDATE [resource].[Resources] SET [Role] = 'DEV' WHERE [Role] = 'Developer';");
            migrationBuilder.Sql("UPDATE [resource].[Resources] SET [Role] = 'DEV' WHERE [Role] = 'Senior Developer';");
            migrationBuilder.Sql("UPDATE [resource].[Resources] SET [Role] = 'DEV' WHERE [Role] = 'Tech Lead';");

            // ── Remove old Roles seed data ──
            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 4);

            // ── Insert new Roles seed data ──
            migrationBuilder.InsertData(
                table: "Roles",
                schema: "resource",
                columns: ["Id", "Code", "DisplayName", "IsActive", "SortOrder"],
                values: new object[,]
                {
                    { 1, "DEV", "Developer", true, 1 },
                    { 2, "QA", "Quality Assurance", true, 2 },
                    { 3, "SA", "System Analyst", true, 3 },
                    { 4, "BA", "Business Analyst", true, 4 },
                    { 5, "UX", "UX Designer", true, 5 },
                    { 6, "UI", "UI Designer", true, 6 }
                });

            // ── Update LookupValues: remove old ResourceRole entries ──
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

            // ── Update LookupValues: remove old WorkingWeek entries (ids 20-21) ──
            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 21);

            // ── Insert new ResourceRole lookup values ──
            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: ["Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder"],
                values: new object[,]
                {
                    { 16, "ResourceRole", "DEV", "Developer", true, 1 },
                    { 17, "ResourceRole", "QA", "Quality Assurance", true, 2 },
                    { 18, "ResourceRole", "SA", "System Analyst", true, 3 },
                    { 19, "ResourceRole", "BA", "Business Analyst", true, 4 },
                    { 20, "ResourceRole", "UX", "UX Designer", true, 5 },
                    { 21, "ResourceRole", "UI", "UI Designer", true, 6 }
                });

            // ── Re-insert WorkingWeek entries with shifted ids ──
            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: ["Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder"],
                values: new object[,]
                {
                    { 22, "WorkingWeek", "Sun-Thu", "Sunday - Thursday", true, 1 },
                    { 23, "WorkingWeek", "Mon-Fri", "Monday - Friday", true, 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Remove shifted WorkingWeek entries ──
            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "LookupValues",
                keyColumn: "Id",
                keyValue: 23);

            // ── Remove new ResourceRole lookup values ──
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

            // ── Restore original WorkingWeek entries ──
            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: ["Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder"],
                values: new object[,]
                {
                    { 20, "WorkingWeek", "Sun-Thu", "Sunday - Thursday", true, 1 },
                    { 21, "WorkingWeek", "Mon-Fri", "Monday - Friday", true, 2 }
                });

            // ── Restore original ResourceRole lookup values ──
            migrationBuilder.InsertData(
                table: "LookupValues",
                columns: ["Id", "Category", "Code", "DisplayName", "IsActive", "SortOrder"],
                values: new object[,]
                {
                    { 16, "ResourceRole", "Developer", "Developer", true, 1 },
                    { 17, "ResourceRole", "Senior Developer", "Senior Developer", true, 2 },
                    { 18, "ResourceRole", "Tech Lead", "Tech Lead", true, 3 },
                    { 19, "ResourceRole", "QA", "QA", true, 4 }
                });

            // ── Remove new Roles seed data ──
            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Roles",
                schema: "resource",
                keyColumn: "Id",
                keyValue: 6);

            // ── Restore original Roles seed data ──
            migrationBuilder.InsertData(
                table: "Roles",
                schema: "resource",
                columns: ["Id", "Code", "DisplayName", "IsActive", "SortOrder"],
                values: new object[,]
                {
                    { 1, "Developer", "Developer", true, 1 },
                    { 2, "Senior Developer", "Senior Developer", true, 2 },
                    { 3, "Tech Lead", "Tech Lead", true, 3 },
                    { 4, "QA", "QA", true, 4 }
                });

            // ── Revert resource data ──
            migrationBuilder.Sql("UPDATE [resource].[Resources] SET [Role] = 'Developer' WHERE [Role] = 'DEV';");
        }
    }
}
