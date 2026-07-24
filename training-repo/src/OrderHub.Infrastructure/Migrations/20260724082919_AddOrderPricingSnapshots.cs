using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPricingSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRateSnapshot",
                table: "Orders",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmountSnapshot",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_DiscountRateSnapshot_Range",
                table: "Orders",
                sql: "[DiscountRateSnapshot] IS NULL OR ([DiscountRateSnapshot] >= 0 AND [DiscountRateSnapshot] <= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_PricingSnapshots_Complete",
                table: "Orders",
                sql: "([DiscountRateSnapshot] IS NULL AND [TotalAmountSnapshot] IS NULL) OR ([DiscountRateSnapshot] IS NOT NULL AND [TotalAmountSnapshot] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_DiscountRateSnapshot_Range",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_PricingSnapshots_Complete",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountRateSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TotalAmountSnapshot",
                table: "Orders");
        }
    }
}
