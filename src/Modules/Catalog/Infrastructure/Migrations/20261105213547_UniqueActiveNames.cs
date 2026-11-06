using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueActiveNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Case-insensitive, whitespace-trimmed uniqueness among active (not soft-deleted) rows. Expression + partial
            // indexes can't be modelled in EF, so they live here; the validators give the friendly 400,
            // these stop concurrent requests slipping a duplicate through (mapped to 409).
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_categories_active_name"
                    ON catalog.categories (lower(btrim("Name")))
                    WHERE NOT "IsDeleted";
                """);
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_products_active_category_name"
                    ON catalog.products ("CategoryId", lower(btrim("Name")))
                    WHERE NOT "IsDeleted";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX catalog."IX_products_active_category_name";""");
            migrationBuilder.Sql("""DROP INDEX catalog."IX_categories_active_name";""");
        }
    }
}
