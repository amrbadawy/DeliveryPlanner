using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "filter");

            migrationBuilder.CreateTable(
                name: "SavedViews",
                schema: "filter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PageKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OwnerKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedViews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_OwnerKey_PageKey",
                schema: "filter",
                table: "SavedViews",
                columns: new[] { "OwnerKey", "PageKey" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_OwnerKey_PageKey_Name",
                schema: "filter",
                table: "SavedViews",
                columns: new[] { "OwnerKey", "PageKey", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedViews",
                schema: "filter");
        }
    }
}
