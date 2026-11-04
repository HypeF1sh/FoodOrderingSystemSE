using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderRestaurantTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RestaurantTableId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RestaurantTableId",
                table: "Orders",
                column: "RestaurantTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_RestaurantTables_RestaurantTableId",
                table: "Orders",
                column: "RestaurantTableId",
                principalTable: "RestaurantTables",
                principalColumn: "RestaurantTableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_RestaurantTables_RestaurantTableId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_RestaurantTableId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RestaurantTableId",
                table: "Orders");
        }
    }
}
