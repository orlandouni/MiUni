using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRagChunksPgvector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:estado_reporte", "Pendiente,Revisado,Descartado")
                .Annotation("Npgsql:Enum:rol_usuario", "Estudiante,Administrador")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:pgrouting", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:estado_reporte", "Pendiente,Revisado,Descartado")
                .OldAnnotation("Npgsql:Enum:rol_usuario", "Estudiante,Administrador")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pgrouting", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "rag_chunk",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    documento_fuente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying", nullable: false),
                    source_url = table.Column<string>(type: "character varying", nullable: false),
                    document_title = table.Column<string>(type: "character varying", nullable: false),
                    category = table.Column<string>(type: "character varying", nullable: true),
                    chunk_index = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    content_sha256 = table.Column<string>(type: "character varying", nullable: false),
                    embedding_model = table.Column<string>(type: "character varying", nullable: true),
                    embedding_dimension = table.Column<int>(type: "integer", nullable: true),
                    embedded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("rag_chunk_pkey", x => x.id);
                    table.ForeignKey(
                        name: "rag_chunk_documento_fuente_id_fkey",
                        column: x => x.documento_fuente_id,
                        principalTable: "documentofuente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
                ALTER TABLE rag_chunk
                ADD COLUMN embedding vector(1024);

                ALTER TABLE rag_chunk
                ADD COLUMN search_tsv tsvector GENERATED ALWAYS AS (
                    to_tsvector('spanish', coalesce(content, ''))
                ) STORED;

                COMMENT ON COLUMN rag_chunk.embedding IS 'Dense bge-m3 embedding. Expected dimension: 1024.';
                COMMENT ON COLUMN rag_chunk.search_tsv IS 'Spanish lexical search vector used by hybrid search.';

                CREATE INDEX idx_rag_chunk_search_tsv
                ON rag_chunk USING gin (search_tsv);

                CREATE INDEX idx_rag_chunk_embedding_hnsw
                ON rag_chunk USING hnsw (embedding vector_cosine_ops);
            ");
            migrationBuilder.CreateIndex(
                name: "idx_rag_chunk_category",
                table: "rag_chunk",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "idx_rag_chunk_content_sha256",
                table: "rag_chunk",
                column: "content_sha256");

            migrationBuilder.CreateIndex(
                name: "idx_rag_chunk_documento_fuente",
                table: "rag_chunk",
                column: "documento_fuente_id");

            migrationBuilder.CreateIndex(
                name: "ux_rag_chunk_source_chunk",
                table: "rag_chunk",
                columns: new[] { "source_url", "chunk_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rag_chunk");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:estado_reporte", "Pendiente,Revisado,Descartado")
                .Annotation("Npgsql:Enum:rol_usuario", "Estudiante,Administrador")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:pgrouting", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:Enum:estado_reporte", "Pendiente,Revisado,Descartado")
                .OldAnnotation("Npgsql:Enum:rol_usuario", "Estudiante,Administrador")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pgrouting", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}


