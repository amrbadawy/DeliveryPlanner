using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedViewIsDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                schema: "filter",
                table: "SavedViews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_OwnerKey_PageKey_IsDefault",
                schema: "filter",
                table: "SavedViews",
                columns: new[] { "OwnerKey", "PageKey", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedViews_OwnerKey_PageKey_IsDefault",
                schema: "filter",
                table: "SavedViews");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                schema: "filter",
                table: "SavedViews");
        }
    }
}
