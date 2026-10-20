using System;

namespace MiUni.Api.Models;

public partial class Ragchunk
{
    public Guid Id { get; set; }

    public Guid DocumentoFuenteId { get; set; }

    public string SourceType { get; set; } = null!;

    public string SourceUrl { get; set; } = null!;

    public string DocumentTitle { get; set; } = null!;

    public string? Category { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = null!;

    public string Metadata { get; set; } = "{}";

    public string ContentSha256 { get; set; } = null!;

    public string? EmbeddingModel { get; set; }

    public int? EmbeddingDimension { get; set; }

    public DateTimeOffset? EmbeddedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public virtual Documentofuente DocumentoFuente { get; set; } = null!;
}

