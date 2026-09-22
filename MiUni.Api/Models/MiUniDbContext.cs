using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Identity;
using Microsoft.AspNetCore.Identity;

namespace MiUni.Api.Models;

public partial class MiUniDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public MiUniDbContext()
    {
    }

    public MiUniDbContext(DbContextOptions<MiUniDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Camino> Caminos { get; set; }

    public virtual DbSet<Carrera> Carreras { get; set; }

    public virtual DbSet<Categorium> Categoria { get; set; }

    public virtual DbSet<Documentofuente> Documentofuentes { get; set; }

    public virtual DbSet<Etiquetum> Etiqueta { get; set; }

    public virtual DbSet<Eventotemporal> Eventotemporals { get; set; }

    public virtual DbSet<Favorito> Favoritos { get; set; }

    public virtual DbSet<Foto> Fotos { get; set; }

    public virtual DbSet<Historialchat> Historialchats { get; set; }

    public virtual DbSet<Horariooperacion> Horariooperacions { get; set; }

    public virtual DbSet<Lugar> Lugars { get; set; }

    public virtual DbSet<Productomenu> Productomenus { get; set; }

    public virtual DbSet<Puntointeresinterno> Puntointeresinternos { get; set; }

    public virtual DbSet<Reporteusuario> Reporteusuarios { get; set; }

    public virtual DbSet<Resena> Resenas { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder
            .HasPostgresEnum("estado_reporte", new[] { "Pendiente", "Revisado", "Descartado" })
            .HasPostgresEnum("rol_usuario", new[] { "Estudiante", "Administrador" })
            .HasPostgresExtension("pgcrypto")
            .HasPostgresExtension("pgrouting")
            .HasPostgresExtension("postgis");

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.Nombre)
                .HasColumnName("Nombre");

            entity.Property(e => e.CarreraId)
                .HasColumnName("CarreraId");

            entity.Property(e => e.Semestre)
                .HasColumnName("Semestre");

            entity.Property(e => e.FechaRegistro)
                .HasColumnName("FechaRegistro")
                .HasDefaultValueSql("now()");

            entity.Property(e => e.Rol)
                .HasColumnName("Rol")
                .HasConversion<string>();

            entity.HasOne(e => e.Carrera)
                .WithMany(c => c.Usuarios)
                .HasForeignKey(e => e.CarreraId)
                .HasConstraintName("AspNetUsers_CarreraId_fkey");
        });

        modelBuilder.Entity<Carrera>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("carrera_pkey");

            entity.ToTable("carrera");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Facultad)
                .HasColumnType("character varying")
                .HasColumnName("facultad");
            entity.Property(e => e.Nombre)
                .HasColumnType("character varying")
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Categorium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categoria_pkey");

            entity.ToTable("categoria");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Icono)
                .HasColumnType("character varying")
                .HasColumnName("icono");
            entity.Property(e => e.Nombre)
                .HasColumnType("character varying")
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Documentofuente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("documentofuente_pkey");

            entity.ToTable("documentofuente");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Referencia)
                .HasColumnType("character varying")
                .HasColumnName("referencia");
            entity.Property(e => e.TipoOrigen)
                .HasColumnType("character varying")
                .HasColumnName("tipo_origen");
            entity.Property(e => e.Titulo)
                .HasColumnType("character varying")
                .HasColumnName("titulo");
            entity.Property(e => e.UltimaActualizacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("ultima_actualizacion");
        });

        modelBuilder.Entity<Etiquetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("etiqueta_pkey");

            entity.ToTable("etiqueta");

            entity.HasIndex(e => e.Nombre, "etiqueta_nombre_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasColumnType("character varying")
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Eventotemporal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("eventotemporal_pkey");

            entity.ToTable("eventotemporal");

            entity.HasIndex(e => e.LugarId, "idx_evento_lugar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.LugarId).HasColumnName("lugar_id");
            entity.Property(e => e.Titulo)
                .HasColumnType("character varying")
                .HasColumnName("titulo");

            entity.HasOne(d => d.Lugar).WithMany(p => p.Eventotemporals)
                .HasForeignKey(d => d.LugarId)
                .HasConstraintName("eventotemporal_lugar_id_fkey");
        });

        modelBuilder.Entity<Favorito>(entity =>
        {
            entity.HasKey(e => new { e.UsuarioId, e.LugarId }).HasName("favorito_pkey");

            entity.ToTable("favorito");

            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.LugarId).HasColumnName("lugar_id");
            entity.Property(e => e.FechaAgregado)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_agregado");

            entity.HasOne(d => d.Lugar).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.LugarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("favorito_lugar_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("favorito_usuario_id_fkey");
        });

        modelBuilder.Entity<Foto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("foto_pkey");

            entity.ToTable("foto");

            entity.HasIndex(e => e.LugarId, "idx_foto_lugar");

            entity.HasIndex(e => e.LugarId, "idx_foto_unica_principal")
                .IsUnique()
                .HasFilter("(es_principal = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.EsPrincipal).HasColumnName("es_principal");
            entity.Property(e => e.FechaSubida)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_subida");
            entity.Property(e => e.LugarId).HasColumnName("lugar_id");
            entity.Property(e => e.StorageKey)
                .HasColumnType("character varying")
                .HasColumnName("storage_key");

            entity.HasOne(d => d.Lugar).WithOne(p => p.Foto)
                .HasForeignKey<Foto>(d => d.LugarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("foto_lugar_id_fkey");
        });

        modelBuilder.Entity<Historialchat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("historialchat_pkey");

            entity.ToTable("historialchat");

            entity.HasIndex(e => e.SesionId, "idx_historial_sesion");

            entity.HasIndex(e => e.UsuarioId, "idx_historial_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Contenido).HasColumnName("contenido");
            entity.Property(e => e.ContextoUtilizado)
                .HasColumnType("jsonb")
                .HasColumnName("contexto_utilizado");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Rol)
                .HasColumnType("character varying")
                .HasColumnName("rol");
            entity.Property(e => e.SesionId).HasColumnName("sesion_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Historialchats)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("historialchat_usuario_id_fkey");
        });

        modelBuilder.Entity<Horariooperacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("horariooperacion_pkey");

            entity.ToTable("horariooperacion");

            entity.HasIndex(e => e.LugarId, "idx_horario_lugar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.DiaSemana).HasColumnName("dia_semana");
            entity.Property(e => e.HoraApertura).HasColumnName("hora_apertura");
            entity.Property(e => e.HoraCierre).HasColumnName("hora_cierre");
            entity.Property(e => e.LugarId).HasColumnName("lugar_id");

            entity.HasOne(d => d.Lugar).WithMany(p => p.Horariooperacions)
                .HasForeignKey(d => d.LugarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("horariooperacion_lugar_id_fkey");
        });

        modelBuilder.Entity<Lugar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lugar_pkey");

            entity.ToTable("lugar");

            entity.HasIndex(e => e.CategoriaId, "idx_lugar_categoria");

            entity.HasIndex(e => e.Ubicacion, "idx_lugar_ubicacion").HasMethod("gist");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Nombre)
                .HasColumnType("character varying")
                .HasColumnName("nombre");
            entity.Property(e => e.Piso).HasColumnName("piso");
            entity.Property(e => e.Ubicacion)
                .HasColumnType("geography(Point,4326)")
                .HasColumnName("ubicacion");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Lugars)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lugar_categoria_id_fkey");

            entity.HasMany(d => d.Tags).WithMany(p => p.Lugars)
                .UsingEntity<Dictionary<string, object>>(
                    "Lugartag",
                    r => r.HasOne<Etiquetum>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("lugartag_tag_id_fkey"),
                    l => l.HasOne<Lugar>().WithMany()
                        .HasForeignKey("LugarId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("lugartag_lugar_id_fkey"),
                    j =>
                    {
                        j.HasKey("LugarId", "TagId").HasName("lugartag_pkey");
                        j.ToTable("lugartag");
                        j.IndexerProperty<Guid>("LugarId").HasColumnName("lugar_id");
                        j.IndexerProperty<Guid>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<Productomenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("productomenu_pkey");

            entity.ToTable("productomenu");

            entity.HasIndex(e => e.LugarId, "idx_producto_lugar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .HasColumnType("character varying")
                .HasColumnName("descripcion");
            entity.Property(e => e.EstaDisponible)
                .HasDefaultValue(true)
                .HasColumnName("esta_disponible");
            entity.Property(e => e.LugarId).HasColumnName("lugar_id");
            entity.Property(e => e.Nombre)
                .HasColumnType("character varying")
                .HasColumnName("nombre");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");

            entity.HasOne(d => d.Lugar).WithMany(p => p.Productomenus)
                .HasForeignKey(d => d.LugarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("productomenu_lugar_id_fkey");
        });

        modelBuilder.Entity<Puntointeresinterno>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("puntointeresinterno_pkey");

            entity.ToTable("puntointeresinterno");

            entity.HasIndex(e => e.LugarId, "idx_poi_lugar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .HasColumnType("character varying")
                .HasColumnName("descripcion");
            entity.Property(e => e.LugarId).HasColumnName("lugar_id");
            entity.Property(e => e.Piso).HasColumnName("piso");
            entity.Property(e => e.Tipo)
                .HasColumnType("character varying")
                .HasColumnName("tipo");

            entity.HasOne(d => d.Lugar).WithMany(p => p.Puntointeresinternos)
                .HasForeignKey(d => d.LugarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("puntointeresinterno_lugar_id_fkey");
        });

        modelBuilder.Entity<Reporteusuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reporteusuario_pkey");

            entity.ToTable("reporteusuario");

            entity.HasIndex(e => e.LugarId, "idx_reporte_lugar");

            entity.HasIndex(e => e.ResenaId, "idx_reporte_resena");

            entity.HasIndex(e => e.UsuarioId, "idx_reporte_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.LugarId).HasColumnName("lugar_id");
            entity.Property(e => e.Motivo)
                .HasColumnType("character varying")
                .HasColumnName("motivo");
            entity.Property(e => e.ResenaId).HasColumnName("resena_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Lugar).WithMany(p => p.Reporteusuarios)
                .HasForeignKey(d => d.LugarId)
                .HasConstraintName("reporteusuario_lugar_id_fkey");

            entity.HasOne(d => d.Resena).WithMany(p => p.Reporteusuarios)
                .HasForeignKey(d => d.ResenaId)
                .HasConstraintName("reporteusuario_resena_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Reporteusuarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reporteusuario_usuario_id_fkey");
        });

        modelBuilder.Entity<Resena>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("resena_pkey");

            entity.ToTable("resena");

            entity.HasIndex(e => e.LugarId, "idx_resena_lugar");

            entity.HasIndex(e => e.UsuarioId, "idx_resena_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Calificacion).HasColumnName("calificacion");
            entity.Property(e => e.Comentario).HasColumnName("comentario");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.LugarId).HasColumnName("lugar_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Lugar).WithMany(p => p.Resenas)
                .HasForeignKey(d => d.LugarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("resena_lugar_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Resenas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("resena_usuario_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
