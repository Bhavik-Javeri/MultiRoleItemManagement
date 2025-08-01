using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressAndPincodeToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "price",
                table: "CartItems",
                newName: "Price");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Pincode",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "CartItems",
                newName: "price");
        }
    }
}
