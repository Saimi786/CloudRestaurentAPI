using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudRestaurent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "KitchenStationId",
                table: "Categories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KitchenStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenStations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_KitchenStationId",
                table: "Categories",
                column: "KitchenStationId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenStations_BranchId",
                table: "KitchenStations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenStations_TenantId",
                table: "KitchenStations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenStations_TenantId_BranchId_Name",
                table: "KitchenStations",
                columns: new[] { "TenantId", "BranchId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitchenStations");

            migrationBuilder.DropIndex(
                name: "IX_Categories_KitchenStationId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "KitchenStationId",
                table: "Categories");
        }
    }
}
