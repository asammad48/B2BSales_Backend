using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B2BSpareParts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransferSerializedBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedUnitBarcodesJson",
                table: "StockTransferItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedUnitBarcodesJson",
                table: "StockTransferItems");
        }
    }
}
