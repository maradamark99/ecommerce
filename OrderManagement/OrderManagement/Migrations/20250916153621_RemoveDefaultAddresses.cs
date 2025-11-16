using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDetails_Address_DefaultBillingAddressId",
                table: "CustomerDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDetails_Address_DefaultShippingAddressId",
                table: "CustomerDetails");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDetails_DefaultBillingAddressId",
                table: "CustomerDetails");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDetails_DefaultShippingAddressId",
                table: "CustomerDetails");

            migrationBuilder.DropColumn(
                name: "DefaultBillingAddressId",
                table: "CustomerDetails");

            migrationBuilder.DropColumn(
                name: "DefaultShippingAddressId",
                table: "CustomerDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DefaultBillingAddressId",
                table: "CustomerDetails",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DefaultShippingAddressId",
                table: "CustomerDetails",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDetails_DefaultBillingAddressId",
                table: "CustomerDetails",
                column: "DefaultBillingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDetails_DefaultShippingAddressId",
                table: "CustomerDetails",
                column: "DefaultShippingAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDetails_Address_DefaultBillingAddressId",
                table: "CustomerDetails",
                column: "DefaultBillingAddressId",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDetails_Address_DefaultShippingAddressId",
                table: "CustomerDetails",
                column: "DefaultShippingAddressId",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
