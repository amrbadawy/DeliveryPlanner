using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjustmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdjStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdjEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailabilityPct = table.Column<double>(type: "float", nullable: false),
                    AdjType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adjustments_TeamMembers_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "TeamMembers",
                        principalColumn: "ResourceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_ResourceId",
                table: "Adjustments",
                column: "ResourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Adjustments");
        }
    }
}
