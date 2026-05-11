using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudRestaurent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DefaultTimezone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    FiscalYearStartDay = table.Column<int>(type: "int", nullable: false),
                    TaxLabel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DefaultTaxRateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RewardPointsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RewardPointsName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RewardPointsEarnPerCurrency = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RewardPointsRedeemValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RewardPointsMinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RewardPointsExpiryDays = table.Column<int>(type: "int", nullable: true),
                    SalesPrefix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PurchasePrefix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ExpensePrefix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CustomerPrefix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PosShowStockLevel = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSettings_TenantId",
                table: "BusinessSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessSettings");
        }
    }
}
