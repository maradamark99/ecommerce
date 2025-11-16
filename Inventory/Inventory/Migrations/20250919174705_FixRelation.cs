using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Migrations
{
    /// <inheritdoc />
    public partial class FixRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductId1",
                table: "StockReservations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_ProductId",
                table: "StockReservations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_ProductId1",
                table: "StockReservations",
                column: "ProductId1");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockReservations_Product_ProductId",
                table: "StockReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_StockReservations_Product_ProductId1",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_ProductId",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_ProductId1",
                table: "StockReservations");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "StockReservations");
        }
    }
}
