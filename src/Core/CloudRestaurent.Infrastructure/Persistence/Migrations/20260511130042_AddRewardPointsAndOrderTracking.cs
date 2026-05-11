using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudRestaurent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardPointsAndOrderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ----- Customers -----
            // Existing customer balance lives in `LoyaltyPoints`; rename it to the new
            // canonical `TotalRewardPoints` so we don't lose live balances on this upgrade.
            // EF's auto-scaffolder picked the wrong target (TotalRewardPointsUsed), so we
            // override with the correct semantic rename.
            migrationBuilder.RenameColumn(
                name: "LoyaltyPoints",
                table: "Customers",
                newName: "TotalRewardPoints");

            migrationBuilder.AddColumn<int>(
                name: "TotalRewardPointsUsed",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRewardPointsExpired",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ----- Orders -----
            migrationBuilder.AddColumn<int>(
                name: "RewardPointsEarned",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RewardPointsRedeemed",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RewardPointsRedeemedAmount",
                table: "Orders",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // ----- BusinessSettings: drop old shape, add UP-aligned shape -----
            // The old schema had three RP fields modelled differently from UltimatePOS.
            // Drop them — tenants will re-enter their RP config on the new Settings tab.
            // (The new RewardPointsEnabled defaults to false, so nothing earns/redeems
            // until they opt in.)
            migrationBuilder.DropColumn(
                name: "RewardPointsEarnPerCurrency",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "RewardPointsMinOrderAmount",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "RewardPointsExpiryDays",
                table: "BusinessSettings");

            // New UP-aligned columns
            migrationBuilder.AddColumn<decimal>(
                name: "RewardPointsAmountPerPoint",
                table: "BusinessSettings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "RewardPointsMinOrderForEarn",
                table: "BusinessSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RewardPointsMaxPerOrder",
                table: "BusinessSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RewardPointsMinOrderForRedeem",
                table: "BusinessSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RewardPointsMinRedeem",
                table: "BusinessSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardPointsMaxRedeem",
                table: "BusinessSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardPointsExpiryPeriod",
                table: "BusinessSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardPointsExpiryUnit",
                table: "BusinessSettings",
                type: "int",
                nullable: false,
                defaultValue: 2); // 2 = Year (matches the entity default)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalRewardPointsUsed",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TotalRewardPointsExpired",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "TotalRewardPoints",
                table: "Customers",
                newName: "LoyaltyPoints");

            migrationBuilder.DropColumn(name: "RewardPointsEarned", table: "Orders");
            migrationBuilder.DropColumn(name: "RewardPointsRedeemed", table: "Orders");
            migrationBuilder.DropColumn(name: "RewardPointsRedeemedAmount", table: "Orders");

            migrationBuilder.DropColumn(name: "RewardPointsAmountPerPoint", table: "BusinessSettings");
            migrationBuilder.DropColumn(name: "RewardPointsMinOrderForEarn", table: "BusinessSettings");
            migrationBuilder.DropColumn(name: "RewardPointsMaxPerOrder", table: "BusinessSettings");
            migrationBuilder.DropColumn(name: "RewardPointsMinOrderForRedeem", table: "BusinessSettings");
            migrationBuilder.DropColumn(name: "RewardPointsMinRedeem", table: "BusinessSettings");
            migrationBuilder.DropColumn(name: "RewardPointsMaxRedeem", table: "BusinessSettings");
            migrationBuilder.DropColumn(name: "RewardPointsExpiryPeriod", table: "BusinessSettings");
            migrationBuilder.DropColumn(name: "RewardPointsExpiryUnit", table: "BusinessSettings");

            migrationBuilder.AddColumn<decimal>(
                name: "RewardPointsEarnPerCurrency",
                table: "BusinessSettings",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RewardPointsMinOrderAmount",
                table: "BusinessSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RewardPointsExpiryDays",
                table: "BusinessSettings",
                type: "int",
                nullable: true);
        }
    }
}
