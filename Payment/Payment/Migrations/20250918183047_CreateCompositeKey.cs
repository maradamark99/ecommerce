using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Migrations
{
    /// <inheritdoc />
    public partial class CreateCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_CustomerOrderMappings_CustomerOrderMappingId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CustomerOrderMappingId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomerOrderMappings",
                table: "CustomerOrderMappings");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentId",
                table: "CustomerOrderMappings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomerOrderMappings",
                table: "CustomerOrderMappings",
                columns: new[] { "CustomerId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderMappings_PaymentId",
                table: "CustomerOrderMappings",
                column: "PaymentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrderMappings_Payments_PaymentId",
                table: "CustomerOrderMappings",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrderMappings_Payments_PaymentId",
                table: "CustomerOrderMappings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomerOrderMappings",
                table: "CustomerOrderMappings");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrderMappings_PaymentId",
                table: "CustomerOrderMappings");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentId",
                table: "CustomerOrderMappings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomerOrderMappings",
                table: "CustomerOrderMappings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerOrderMappingId",
                table: "Payments",
                column: "CustomerOrderMappingId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_CustomerOrderMappings_CustomerOrderMappingId",
                table: "Payments",
                column: "CustomerOrderMappingId",
                principalTable: "CustomerOrderMappings",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
