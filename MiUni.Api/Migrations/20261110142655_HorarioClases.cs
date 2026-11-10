using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class HorarioClases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "horario_clase",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    materia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    profesor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    dia = table.Column<short>(type: "smallint", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    salon = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("horario_clase_pkey", x => x.id);
                    table.CheckConstraint("horario_clase_dia_check", "dia BETWEEN 1 AND 7");
                    table.CheckConstraint("horario_clase_horas_check", "hora_fin > hora_inicio");
                    table.ForeignKey(
                        name: "horario_clase_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "horario_clase_usuario_dia_idx",
                table: "horario_clase",
                columns: new[] { "usuario_id", "dia", "hora_inicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "horario_clase");
        }
    }
}
