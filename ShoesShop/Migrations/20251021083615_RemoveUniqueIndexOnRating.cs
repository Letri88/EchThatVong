using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoesShop.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueIndexOnRating : Migration
    {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "IX_Ratings_ProductId",
				table: "Ratings");

			migrationBuilder.CreateIndex(
				name: "IX_Ratings_ProductId",
				table: "Ratings",
				column: "ProductId");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "IX_Ratings_ProductId",
				table: "Ratings");

			migrationBuilder.CreateIndex(
				name: "IX_Ratings_ProductId",
				table: "Ratings",
				column: "ProductId",
				unique: true);
		}
	}
}
