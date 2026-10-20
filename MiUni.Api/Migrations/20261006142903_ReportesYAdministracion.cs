using System;
using MiUni.Api.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReportesYAdministracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Admite la migración inicial ("Estado" integer) y la base original
            // que ya usa estado estado_reporte, sin eliminar ni recrear datos.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_schema='public' AND table_name='reporteusuario' AND column_name='Estado') THEN
                        IF EXISTS (SELECT 1 FROM information_schema.columns
                            WHERE table_schema='public' AND table_name='reporteusuario' AND column_name='estado') THEN
                            RAISE EXCEPTION 'Existen Estado y estado: reconciliar las columnas antes de migrar.';
                        END IF;
                        ALTER TABLE public.reporteusuario RENAME COLUMN "Estado" TO estado;
                    END IF;
                    ALTER TABLE public.reporteusuario ALTER COLUMN estado DROP DEFAULT;
                    ALTER TABLE public.reporteusuario ALTER COLUMN estado TYPE estado_reporte
                    USING (CASE estado::text WHEN '0' THEN 'Pendiente' WHEN '1' THEN 'Revisado'
                           WHEN '2' THEN 'Descartado' ELSE estado::text END)::estado_reporte;
                    ALTER TABLE public.reporteusuario ALTER COLUMN estado SET NOT NULL;
                END $$;
                """);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_creacion",
                table: "reporteusuario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_reporte_usuario_lugar",
                table: "reporteusuario",
                columns: new[] { "usuario_id", "lugar_id" },
                unique: true,
                filter: "lugar_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_reporte_usuario_resena",
                table: "reporteusuario",
                columns: new[] { "usuario_id", "resena_id" },
                unique: true,
                filter: "resena_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reporte_destino",
                table: "reporteusuario",
                sql: "(lugar_id IS NOT NULL) <> (resena_id IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_reporte_usuario_lugar",
                table: "reporteusuario");

            migrationBuilder.DropIndex(
                name: "uq_reporte_usuario_resena",
                table: "reporteusuario");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reporte_destino",
                table: "reporteusuario");

            migrationBuilder.DropColumn(
                name: "fecha_creacion",
                table: "reporteusuario");

            migrationBuilder.Sql("""
                ALTER TABLE public.reporteusuario ALTER COLUMN estado TYPE integer
                USING CASE estado::text WHEN 'Pendiente' THEN 0 WHEN 'Revisado' THEN 1 WHEN 'Descartado' THEN 2 END;
                ALTER TABLE public.reporteusuario RENAME COLUMN estado TO "Estado";
                """);
        }
    }
}
