using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudRestaurent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KitchenTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReadyAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ServedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenTickets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenTickets_OpenedAt",
                table: "KitchenTickets",
                column: "OpenedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenTickets_OrderId",
                table: "KitchenTickets",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenTickets_TenantId",
                table: "KitchenTickets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenTickets_TenantId_BranchId_Status",
                table: "KitchenTickets",
                columns: new[] { "TenantId", "BranchId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitchenTickets");
        }
    }
}
