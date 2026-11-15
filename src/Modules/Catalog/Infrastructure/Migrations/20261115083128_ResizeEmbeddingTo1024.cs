using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <summary>
    /// Resizes products.Embedding from vector(1536) (OpenAI text-embedding-3-small) to vector(1024)
    /// (Ollama mxbai-embed-large). pgvector cannot cast between dimensions, so existing vectors are
    /// discarded; products must be re-embedded afterwards.
    /// </summary>
    public partial class ResizeEmbeddingTo1024 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => Resize(migrationBuilder, 1024);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => Resize(migrationBuilder, 1536);

        private static void Resize(MigrationBuilder migrationBuilder, int dimensions)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS catalog."IX_products_Embedding";""");

            migrationBuilder.Sql($"""
                ALTER TABLE catalog.products
                    ALTER COLUMN "Embedding" TYPE vector({dimensions}) USING NULL;
                """);

            // Cleared vectors are stale; null the timestamp too so nothing treats them as fresh.
            migrationBuilder.Sql("""UPDATE catalog.products SET "EmbeddingGeneratedAt" = NULL;""");

            migrationBuilder.Sql("""
                CREATE INDEX "IX_products_Embedding"
                    ON catalog.products USING ivfflat ("Embedding" vector_cosine_ops);
                """);
        }
    }
}
