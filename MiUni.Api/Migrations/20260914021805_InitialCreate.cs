using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:estado_reporte", "Pendiente,Revisado,Descartado")
                .Annotation("Npgsql:Enum:rol_usuario", "Estudiante,Administrador")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:pgrouting", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "camino",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    geometria = table.Column<LineString>(type: "geography(LineString,4326)", nullable: false),
                    transitable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    tipo_superficie = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("camino_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "carrera",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nombre = table.Column<string>(type: "character varying", nullable: false),
                    facultad = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("carrera_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nombre = table.Column<string>(type: "character varying", nullable: false),
                    icono = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("categoria_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documentofuente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    titulo = table.Column<string>(type: "character varying", nullable: false),
                    tipo_origen = table.Column<string>(type: "character varying", nullable: false),
                    referencia = table.Column<string>(type: "character varying", nullable: false),
                    ultima_actualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("documentofuente_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "etiqueta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nombre = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("etiqueta_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nombre = table.Column<string>(type: "character varying", nullable: false),
                    correo_institucional = table.Column<string>(type: "character varying", nullable: false),
                    password_hash = table.Column<string>(type: "character varying", nullable: false),
                    carrera_id = table.Column<Guid>(type: "uuid", nullable: true),
                    semestre = table.Column<short>(type: "smallint", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    Rol = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("usuario_pkey", x => x.id);
                    table.ForeignKey(
                        name: "usuario_carrera_id_fkey",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lugar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nombre = table.Column<string>(type: "character varying", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    ubicacion = table.Column<Point>(type: "geography(Point,4326)", nullable: false),
                    piso = table.Column<short>(type: "smallint", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("lugar_pkey", x => x.id);
                    table.ForeignKey(
                        name: "lugar_categoria_id_fkey",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "historialchat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "character varying", nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false),
                    contexto_utilizado = table.Column<string>(type: "jsonb", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("historialchat_pkey", x => x.id);
                    table.ForeignKey(
                        name: "historialchat_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "eventotemporal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    titulo = table.Column<string>(type: "character varying", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("eventotemporal_pkey", x => x.id);
                    table.ForeignKey(
                        name: "eventotemporal_lugar_id_fkey",
                        column: x => x.lugar_id,
                        principalTable: "lugar",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "favorito",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_agregado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("favorito_pkey", x => new { x.usuario_id, x.lugar_id });
                    table.ForeignKey(
                        name: "favorito_lugar_id_fkey",
                        column: x => x.lugar_id,
                        principalTable: "lugar",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "favorito_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "foto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying", nullable: false),
                    es_principal = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_subida = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("foto_pkey", x => x.id);
                    table.ForeignKey(
                        name: "foto_lugar_id_fkey",
                        column: x => x.lugar_id,
                        principalTable: "lugar",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "horariooperacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dia_semana = table.Column<short>(type: "smallint", nullable: false),
                    hora_apertura = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_cierre = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("horariooperacion_pkey", x => x.id);
                    table.ForeignKey(
                        name: "horariooperacion_lugar_id_fkey",
                        column: x => x.lugar_id,
                        principalTable: "lugar",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lugartag",
                columns: table => new
                {
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("lugartag_pkey", x => new { x.lugar_id, x.tag_id });
                    table.ForeignKey(
                        name: "lugartag_lugar_id_fkey",
                        column: x => x.lugar_id,
                        principalTable: "lugar",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "lugartag_tag_id_fkey",
                        column: x => x.tag_id,
                        principalTable: "etiqueta",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "productomenu",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying", nullable: false),
                    descripcion = table.Column<string>(type: "character varying", nullable: true),
                    precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    esta_disponible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("productomenu_pkey", x => x.id);
                    table.ForeignKey(
                        name: "productomenu_lugar_id_fkey",
                        column: x => x.lugar_id,
                        principalTable: "lugar",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "puntointeresinterno",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying", nullable: false),
                    piso = table.Column<short>(type: "smallint", nullable: true),
                    descripcion = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("puntointeresinterno_pkey", x => x.id);
                    table.ForeignKey(
                        name: "puntointeresinterno_lugar_id_fkey",
                        column: x => x.lugar_id,
                        principalTable: "lugar",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "resena",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    calificacion = table.Column<short>(type: "smallint", nullable: false),
                    comentario = table.Column<string>(type: "text", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("resena_pkey", x => x.id);
                    table.ForeignKey(
                        name: "resena_lugar_id_fkey",
                        column: x => x.lugar_id,
                        principalTable: "lugar",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "resena_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reporteusuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resena_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo = table.Column<string>(type: "character varying", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("reporteusuario_pkey", x => x.id);
                    table.ForeignKey(
                        name: "reporteusuario_lugar_id_fkey",
                        column: x => x.lugar_id,
                        principalTable: "lugar",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "reporteusuario_resena_id_fkey",
                        column: x => x.resena_id,
                        principalTable: "resena",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "reporteusuario_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_camino_geometria",
                table: "camino",
                column: "geometria")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "etiqueta_nombre_key",
                table: "etiqueta",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_evento_lugar",
                table: "eventotemporal",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "IX_favorito_lugar_id",
                table: "favorito",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "idx_foto_lugar",
                table: "foto",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "idx_foto_unica_principal",
                table: "foto",
                column: "lugar_id",
                unique: true,
                filter: "(es_principal = true)");

            migrationBuilder.CreateIndex(
                name: "idx_historial_sesion",
                table: "historialchat",
                column: "sesion_id");

            migrationBuilder.CreateIndex(
                name: "idx_historial_usuario",
                table: "historialchat",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_horario_lugar",
                table: "horariooperacion",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "idx_lugar_categoria",
                table: "lugar",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "idx_lugar_ubicacion",
                table: "lugar",
                column: "ubicacion")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_lugartag_tag_id",
                table: "lugartag",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "idx_producto_lugar",
                table: "productomenu",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "idx_poi_lugar",
                table: "puntointeresinterno",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_lugar",
                table: "reporteusuario",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_resena",
                table: "reporteusuario",
                column: "resena_id");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_usuario",
                table: "reporteusuario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_resena_lugar",
                table: "resena",
                column: "lugar_id");

            migrationBuilder.CreateIndex(
                name: "idx_resena_usuario",
                table: "resena",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_usuario_carrera",
                table: "usuario",
                column: "carrera_id");

            migrationBuilder.CreateIndex(
                name: "usuario_correo_institucional_key",
                table: "usuario",
                column: "correo_institucional",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "camino");

            migrationBuilder.DropTable(
                name: "documentofuente");

            migrationBuilder.DropTable(
                name: "eventotemporal");

            migrationBuilder.DropTable(
                name: "favorito");

            migrationBuilder.DropTable(
                name: "foto");

            migrationBuilder.DropTable(
                name: "historialchat");

            migrationBuilder.DropTable(
                name: "horariooperacion");

            migrationBuilder.DropTable(
                name: "lugartag");

            migrationBuilder.DropTable(
                name: "productomenu");

            migrationBuilder.DropTable(
                name: "puntointeresinterno");

            migrationBuilder.DropTable(
                name: "reporteusuario");

            migrationBuilder.DropTable(
                name: "etiqueta");

            migrationBuilder.DropTable(
                name: "resena");

            migrationBuilder.DropTable(
                name: "lugar");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "carrera");
        }
    }
}
