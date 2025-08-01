using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemManagement.Migrations
{
    /// <inheritdoc />
    public partial class categoryToCategoryType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "category",
                table: "Items",
                newName: "categoryType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "categoryType",
                table: "Items",
                newName: "category");
        }
    }
}
