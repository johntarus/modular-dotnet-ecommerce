using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillProductSkuAndCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddedSkuAndCurrency added both columns with an empty-string default,
            // leaving existing products with no currency (which breaks order pricing) and no SKU.
            migrationBuilder.Sql("""
                UPDATE catalog.products
                SET "Currency" = 'KES'
                WHERE "Currency" = '';

                UPDATE catalog.products
                SET "Sku" = 'SKU-' || upper(left(replace("Id"::text, '-', ''), 8))
                WHERE "Sku" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
