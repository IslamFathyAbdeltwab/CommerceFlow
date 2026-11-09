using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributeValues_ProductAttributeId",
                table: "ProductVariantAttributeValues",
                column: "ProductAttributeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Categories_CategoryId",
                table: "ProductCategories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariantAttributeValues_ProductAttributes_ProductAttributeId",
                table: "ProductVariantAttributeValues",
                column: "ProductAttributeId",
                principalTable: "ProductAttributes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_Categories_CategoryId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariantAttributeValues_ProductAttributes_ProductAttributeId",
                table: "ProductVariantAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariantAttributeValues_ProductAttributeId",
                table: "ProductVariantAttributeValues");
        }
    }
}
