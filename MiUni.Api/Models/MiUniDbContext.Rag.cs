using Microsoft.EntityFrameworkCore;

namespace MiUni.Api.Models;

public partial class MiUniDbContext
{
    public virtual DbSet<Ragchunk> Ragchunks { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Ragchunk>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rag_chunk_pkey");

            entity.ToTable("rag_chunk");

            entity.HasIndex(e => e.DocumentoFuenteId, "idx_rag_chunk_documento_fuente");

            entity.HasIndex(e => e.Category, "idx_rag_chunk_category");

            entity.HasIndex(e => e.ContentSha256, "idx_rag_chunk_content_sha256");

            entity.HasIndex(e => new { e.SourceUrl, e.ChunkIndex }, "ux_rag_chunk_source_chunk")
                .IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.DocumentoFuenteId).HasColumnName("documento_fuente_id");
            entity.Property(e => e.SourceType)
                .HasColumnType("character varying")
                .HasColumnName("source_type");
            entity.Property(e => e.SourceUrl)
                .HasColumnType("character varying")
                .HasColumnName("source_url");
            entity.Property(e => e.DocumentTitle)
                .HasColumnType("character varying")
                .HasColumnName("document_title");
            entity.Property(e => e.Category)
                .HasColumnType("character varying")
                .HasColumnName("category");
            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.ContentSha256)
                .HasColumnType("character varying")
                .HasColumnName("content_sha256");
            entity.Property(e => e.EmbeddingModel)
                .HasColumnType("character varying")
                .HasColumnName("embedding_model");
            entity.Property(e => e.EmbeddingDimension).HasColumnName("embedding_dimension");
            entity.Property(e => e.EmbeddedAt).HasColumnName("embedded_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.DocumentoFuente).WithMany(p => p.Ragchunks)
                .HasForeignKey(d => d.DocumentoFuenteId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("rag_chunk_documento_fuente_id_fkey");
        });
    }
}


