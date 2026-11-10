using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class OrdenarClaveProfesorBuhoRater : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La clave ahora lleva las palabras ordenadas (ver BuhoRater.Clave), para que
            // "SOTO MORA CLAUDIA PAULINA" y "CLAUDIA PAULINA SOTO MORA" sean el mismo profe.
            migrationBuilder.Sql("""
                WITH n AS (
                    SELECT clave,
                           (SELECT string_agg(w, ' ' ORDER BY w COLLATE "C")
                              FROM unnest(string_to_array(clave, ' ')) AS w WHERE w <> '') AS nueva
                      FROM profesor_buhorater)
                DELETE FROM profesor_buhorater p USING n
                 WHERE p.clave = n.clave
                   AND EXISTS (SELECT 1 FROM n AS otra WHERE otra.nueva = n.nueva AND otra.clave < n.clave);

                UPDATE profesor_buhorater
                   SET clave = (SELECT string_agg(w, ' ' ORDER BY w COLLATE "C")
                                  FROM unnest(string_to_array(clave, ' ')) AS w WHERE w <> '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se puede recuperar el orden original de las palabras; las claves ordenadas siguen funcionando.
        }
    }
}
