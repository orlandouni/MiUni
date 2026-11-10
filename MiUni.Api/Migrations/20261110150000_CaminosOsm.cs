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
            migrationBuilder.AddColumn<long>(
                name: "osm_way_id",
                table: "camino",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_camino_osm_way_id",
                table: "camino",
                column: "osm_way_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_camino_osm_way_id", table: "camino");
            migrationBuilder.DropColumn(name: "osm_way_id", table: "camino");
        }
    }
}
