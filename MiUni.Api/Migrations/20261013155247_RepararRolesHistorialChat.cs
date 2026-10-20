using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class RepararRolesHistorialChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE historialchat
                DROP CONSTRAINT IF EXISTS historialchat_rol_check;

                UPDATE historialchat
                SET rol = CASE lower(trim(rol))
                    WHEN 'usuario' THEN 'user'
                    WHEN 'asistente' THEN 'assistant'
                    ELSE rol
                END;
                """);
            migrationBuilder.AddCheckConstraint(
                name: "historialchat_rol_check", table: "historialchat",
                sql: "rol IN ('user', 'assistant')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "historialchat_rol_check", table: "historialchat");
            migrationBuilder.Sql("""
                UPDATE historialchat
                SET rol = CASE lower(trim(rol))
                    WHEN 'user' THEN 'usuario'
                    WHEN 'assistant' THEN 'asistente'
                    ELSE rol
                END;
                """);
            migrationBuilder.AddCheckConstraint(
                name: "historialchat_rol_check", table: "historialchat",
                sql: "rol IN ('usuario', 'asistente')");
        }
    }
}
