using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B2BSpareParts.Infrastructure.Migrations
{
    public partial class AddOrderItemSelectedUnitBarcodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedUnitBarcodesJson",
                table: "OrderItems",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedUnitBarcodesJson",
                table: "OrderItems");
        }
    }
}
