using Microsoft.EntityFrameworkCore;

namespace MiUni.Api.Models;

public partial class MiUniDbContext
{
    public virtual DbSet<KardexMateria> KardexMaterias { get; set; }

    private static void ConfigurarKardex(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KardexMateria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("kardex_materia_pkey");

            entity.ToTable("kardex_materia", t =>
            {
                t.HasCheckConstraint("kardex_materia_calificacion_check", "calificacion BETWEEN 0 AND 100");
                t.HasCheckConstraint("kardex_materia_creditos_check", "creditos BETWEEN 0 AND 30");
                t.HasCheckConstraint("kardex_materia_semestre_check", "semestre BETWEEN 1 AND 12");
            });

            entity.HasIndex(e => new { e.UsuarioId, e.Semestre }, "kardex_materia_usuario_semestre_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.Materia)
                .HasMaxLength(120)
                .HasColumnName("materia");
            entity.Property(e => e.Clave)
                .HasMaxLength(20)
                .HasColumnName("clave");
            entity.Property(e => e.Periodo)
                .HasMaxLength(20)
                .HasColumnName("periodo");
            entity.Property(e => e.Semestre).HasColumnName("semestre");
            entity.Property(e => e.Calificacion)
                .HasPrecision(5, 2)
                .HasColumnName("calificacion");
            entity.Property(e => e.Creditos).HasColumnName("creditos");
            entity.Property(e => e.Estado)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("estado");

            entity.HasOne(d => d.Usuario).WithMany(p => p.KardexMaterias)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("kardex_materia_usuario_id_fkey");
        });
    }
}
