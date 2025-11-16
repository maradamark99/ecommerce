using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryEntries_Product_ProductId",
                table: "InventoryEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_StockReservations_Product_ProductId",
                table: "StockReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_StockReservations_Product_ProductId1",
                table: "StockReservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Product",
                table: "Product");

            migrationBuilder.RenameTable(
                name: "Product",
                newName: "Products");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryEntries_Products_ProductId",
                table: "InventoryEntries",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockReservations_Products_ProductId",
                table: "StockReservations",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockReservations_Products_ProductId1",
                table: "StockReservations",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryEntries_Products_ProductId",
                table: "InventoryEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_StockReservations_Products_ProductId",
                table: "StockReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_StockReservations_Products_ProductId1",
                table: "StockReservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "Product");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Product",
                table: "Product",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryEntries_Product_ProductId",
                table: "InventoryEntries",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockReservations_Product_ProductId",
                table: "StockReservations",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockReservations_Product_ProductId1",
                table: "StockReservations",
                column: "ProductId1",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
