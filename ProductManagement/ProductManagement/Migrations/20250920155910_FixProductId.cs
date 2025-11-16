using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagement.Migrations
{
    /// <inheritdoc />
    public partial class FixProductId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductMedia_Products_ProductId1",
                table: "ProductMedia");

            migrationBuilder.DropIndex(
                name: "IX_ProductMedia_ProductId1",
                table: "ProductMedia");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "ProductMedia");

            migrationBuilder.AlterColumn<string>(
                name: "ProductId",
                table: "ProductMedia",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMedia_ProductId",
                table: "ProductMedia",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMedia_Products_ProductId",
                table: "ProductMedia",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductMedia_Products_ProductId",
                table: "ProductMedia");

            migrationBuilder.DropIndex(
                name: "IX_ProductMedia_ProductId",
                table: "ProductMedia");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductMedia",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ProductId1",
                table: "ProductMedia",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductMedia_ProductId1",
                table: "ProductMedia",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMedia_Products_ProductId1",
                table: "ProductMedia",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
