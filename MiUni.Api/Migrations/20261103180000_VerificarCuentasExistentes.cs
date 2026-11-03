using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MiUni.Api.Models;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <summary>
    /// La verificación de correo se exige desde esta versión. Las cuentas creadas antes
    /// se marcan como verificadas para no bloquear a usuarios ni administradores existentes.
    /// </summary>
    [DbContext(typeof(MiUniDbContext))]
    [Migration("20261103180000_VerificarCuentasExistentes")]
    public partial class VerificarCuentasExistentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "AspNetUsers" SET "EmailConfirmed" = TRUE WHERE NOT "EmailConfirmed";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se puede saber qué cuentas no estaban verificadas; no se revierte.
        }
    }
}
