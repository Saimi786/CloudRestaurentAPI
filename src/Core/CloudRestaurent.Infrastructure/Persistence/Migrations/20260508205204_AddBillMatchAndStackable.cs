using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudRestaurent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillMatchAndStackable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscrepancyAmount",
                table: "SupplierBills",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscrepancyReason",
                table: "SupplierBills",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAmount",
                table: "SupplierBills",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchStatus",
                table: "SupplierBills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MatchedAt",
                table: "SupplierBills",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MatchedByUserId",
                table: "SupplierBills",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Stackable",
                table: "MixMatchGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBills_MatchStatus",
                table: "SupplierBills",
                column: "MatchStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierBills_MatchStatus",
                table: "SupplierBills");

            migrationBuilder.DropColumn(
                name: "DiscrepancyAmount",
                table: "SupplierBills");

            migrationBuilder.DropColumn(
                name: "DiscrepancyReason",
                table: "SupplierBills");

            migrationBuilder.DropColumn(
                name: "ExpectedAmount",
                table: "SupplierBills");

            migrationBuilder.DropColumn(
                name: "MatchStatus",
                table: "SupplierBills");

            migrationBuilder.DropColumn(
                name: "MatchedAt",
                table: "SupplierBills");

            migrationBuilder.DropColumn(
                name: "MatchedByUserId",
                table: "SupplierBills");

            migrationBuilder.DropColumn(
                name: "Stackable",
                table: "MixMatchGroups");
        }
    }
}
