using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudyGod.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCommerceIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Products",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Price_NonNegative",
                table: "Products",
                sql: "\"Price\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Quantity_NonNegative",
                table: "Products",
                sql: "\"Quantity\" IS NULL OR \"Quantity\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Rating_Range",
                table: "Products",
                sql: "\"Rating\" IS NULL OR (\"Rating\" >= 0 AND \"Rating\" <= 5)");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaystackReference",
                table: "Orders",
                column: "PaystackReference",
                unique: true,
                filter: "\"PaystackReference\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Amounts_NonNegative",
                table: "Orders",
                sql: "\"Subtotal\" >= 0 AND \"ShippingCost\" >= 0 AND \"Total\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Total_EqualsComponents",
                table: "Orders",
                sql: "\"Total\" = \"Subtotal\" + \"ShippingCost\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Price_NonNegative",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Quantity_NonNegative",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Rating_Range",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaystackReference",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Amounts_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Total_EqualsComponents",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Products");
        }
    }
}
