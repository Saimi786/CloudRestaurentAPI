using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudRestaurent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMixMatchGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MixMatchGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DaysOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MixMatchGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MixMatchProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MixMatchGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MixMatchProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MixMatchProducts_MixMatchGroups_MixMatchGroupId",
                        column: x => x.MixMatchGroupId,
                        principalTable: "MixMatchGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MixMatchGroups_IsActive",
                table: "MixMatchGroups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MixMatchGroups_TenantId",
                table: "MixMatchGroups",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MixMatchGroups_TenantId_Name",
                table: "MixMatchGroups",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MixMatchProducts_MixMatchGroupId",
                table: "MixMatchProducts",
                column: "MixMatchGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MixMatchProducts_MixMatchGroupId_ProductId",
                table: "MixMatchProducts",
                columns: new[] { "MixMatchGroupId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MixMatchProducts_ProductId",
                table: "MixMatchProducts",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MixMatchProducts");

            migrationBuilder.DropTable(
                name: "MixMatchGroups");
        }
    }
}
