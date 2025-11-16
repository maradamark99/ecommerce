using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Review.Migrations
{
    /// <inheritdoc />
    public partial class UseDateTimeOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProductListed",
                table: "Reviews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProductListed",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
