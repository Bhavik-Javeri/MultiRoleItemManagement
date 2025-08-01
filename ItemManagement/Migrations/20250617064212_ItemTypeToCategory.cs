using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemManagement.Migrations
{
    /// <inheritdoc />
    public partial class ItemTypeToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Items",
                newName: "category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "category",
                table: "Items",
                newName: "Type");
        }
    }
}
