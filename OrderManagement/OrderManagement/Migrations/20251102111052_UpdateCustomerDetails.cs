using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CustomerDetails_CustomerDetailsCustomerId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerDetailsCustomerId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomerDetails",
                table: "CustomerDetails");

            migrationBuilder.DropColumn(
                name: "CustomerDetailsCustomerId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "OrderId",
                table: "CustomerDetails",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomerDetails",
                table: "CustomerDetails",
                columns: new[] { "CustomerId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDetails_OrderId",
                table: "CustomerDetails",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDetails_Orders_OrderId",
                table: "CustomerDetails",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDetails_Orders_OrderId",
                table: "CustomerDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomerDetails",
                table: "CustomerDetails");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDetails_OrderId",
                table: "CustomerDetails");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "CustomerDetails");

            migrationBuilder.AddColumn<string>(
                name: "CustomerDetailsCustomerId",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomerDetails",
                table: "CustomerDetails",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerDetailsCustomerId",
                table: "Orders",
                column: "CustomerDetailsCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CustomerDetails_CustomerDetailsCustomerId",
                table: "Orders",
                column: "CustomerDetailsCustomerId",
                principalTable: "CustomerDetails",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
