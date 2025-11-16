using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class Modified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeDefinition_Categories_CategoryId",
                table: "AttributeDefinition");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttribute_Products_ProductId",
                table: "ProductAttribute");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductListing_Products_ProductId",
                table: "ProductListing");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductListing",
                table: "ProductListing");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductAttribute",
                table: "ProductAttribute");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttributeDefinition",
                table: "AttributeDefinition");

            migrationBuilder.RenameTable(
                name: "ProductListing",
                newName: "ProductListings");

            migrationBuilder.RenameTable(
                name: "ProductAttribute",
                newName: "ProductAttributes");

            migrationBuilder.RenameTable(
                name: "AttributeDefinition",
                newName: "AttributeDefinitions");

            migrationBuilder.RenameIndex(
                name: "IX_ProductListing_ProductId",
                table: "ProductListings",
                newName: "IX_ProductListings_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductAttribute_ProductId",
                table: "ProductAttributes",
                newName: "IX_ProductAttributes_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_AttributeDefinition_CategoryId",
                table: "AttributeDefinitions",
                newName: "IX_AttributeDefinitions_CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductListings",
                table: "ProductListings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductAttributes",
                table: "ProductAttributes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttributeDefinitions",
                table: "AttributeDefinitions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeDefinitions_Categories_CategoryId",
                table: "AttributeDefinitions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_Products_ProductId",
                table: "ProductAttributes",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductListings_Products_ProductId",
                table: "ProductListings",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeDefinitions_Categories_CategoryId",
                table: "AttributeDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_Products_ProductId",
                table: "ProductAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductListings_Products_ProductId",
                table: "ProductListings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductListings",
                table: "ProductListings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductAttributes",
                table: "ProductAttributes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttributeDefinitions",
                table: "AttributeDefinitions");

            migrationBuilder.RenameTable(
                name: "ProductListings",
                newName: "ProductListing");

            migrationBuilder.RenameTable(
                name: "ProductAttributes",
                newName: "ProductAttribute");

            migrationBuilder.RenameTable(
                name: "AttributeDefinitions",
                newName: "AttributeDefinition");

            migrationBuilder.RenameIndex(
                name: "IX_ProductListings_ProductId",
                table: "ProductListing",
                newName: "IX_ProductListing_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductAttributes_ProductId",
                table: "ProductAttribute",
                newName: "IX_ProductAttribute_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_AttributeDefinitions_CategoryId",
                table: "AttributeDefinition",
                newName: "IX_AttributeDefinition_CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductListing",
                table: "ProductListing",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductAttribute",
                table: "ProductAttribute",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttributeDefinition",
                table: "AttributeDefinition",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeDefinition_Categories_CategoryId",
                table: "AttributeDefinition",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttribute_Products_ProductId",
                table: "ProductAttribute",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductListing_Products_ProductId",
                table: "ProductListing",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
