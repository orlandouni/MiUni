using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MiUni.Api.Models;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <summary>
    /// Marca los caminos importados de OpenStreetMap para poder reimportarlos
    /// sin borrar los que se dibujen a mano (osm_way_id nulo).
    /// </summary>
    [DbContext(typeof(MiUniDbContext))]
    [Migration("20261110150000_CaminosOsm")]
    public partial class CaminosOsm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente: hay bases locales donde la columna o el índice ya se crearon a mano
            // al probar la importación de OSM. No toca los tramos que ya existen.
            migrationBuilder.Sql("ALTER TABLE camino ADD COLUMN IF NOT EXISTS osm_way_id bigint;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_camino_osm_way_id ON camino (osm_way_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_camino_osm_way_id;");
            migrationBuilder.Sql("ALTER TABLE camino DROP COLUMN IF EXISTS osm_way_id;");
        }
    }
}
