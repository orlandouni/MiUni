using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class KardexMaterias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kardex_materia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    materia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    clave = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    periodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    semestre = table.Column<short>(type: "smallint", nullable: true),
                    calificacion = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    creditos = table.Column<short>(type: "smallint", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("kardex_materia_pkey", x => x.id);
                    table.CheckConstraint("kardex_materia_calificacion_check", "calificacion BETWEEN 0 AND 100");
                    table.CheckConstraint("kardex_materia_creditos_check", "creditos BETWEEN 0 AND 30");
                    table.CheckConstraint("kardex_materia_semestre_check", "semestre BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "kardex_materia_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "kardex_materia_usuario_semestre_idx",
                table: "kardex_materia",
                columns: new[] { "usuario_id", "semestre" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kardex_materia");
        }
    }
}
