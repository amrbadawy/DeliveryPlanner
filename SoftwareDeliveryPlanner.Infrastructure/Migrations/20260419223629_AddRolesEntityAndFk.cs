using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoftwareDeliveryPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesEntityAndFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "resource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.UniqueConstraint("AK_Roles_Code", x => x.Code);
                });

            migrationBuilder.InsertData(
                schema: "resource",
                table: "Roles",
                columns: new[] { "Id", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Developer", "Developer", true, 1 },
                    { 2, "Senior Developer", "Senior Developer", true, 2 },
                    { 3, "Tech Lead", "Tech Lead", true, 3 },
                    { 4, "QA", "QA", true, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_Role",
                schema: "resource",
                table: "TeamMembers",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                schema: "resource",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMembers_Roles_Role",
                schema: "resource",
                table: "TeamMembers",
                column: "Role",
                principalSchema: "resource",
                principalTable: "Roles",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamMembers_Roles_Role",
                schema: "resource",
                table: "TeamMembers");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "resource");

            migrationBuilder.DropIndex(
                name: "IX_TeamMembers_Role",
                schema: "resource",
                table: "TeamMembers");
        }
    }
}
