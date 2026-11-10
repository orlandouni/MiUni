using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProfesorBuhoRater : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profesor_buhorater",
                columns: table => new
                {
                    clave = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    perfil_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actualizado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_actualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("profesor_buhorater_pkey", x => x.clave);
                    table.ForeignKey(
                        name: "profesor_buhorater_actualizado_por_id_fkey",
                        column: x => x.actualizado_por_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profesor_buhorater_actualizado_por_id",
                table: "profesor_buhorater",
                column: "actualizado_por_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profesor_buhorater");
        }
    }
}
