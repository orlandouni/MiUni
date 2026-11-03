using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MiUni.Api.Models;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MiUniDbContext))]
    [Migration("20261103160000_NegociosAmbulantes")]
    public partial class NegociosAmbulantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "es_ambulante",
                table: "lugar",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "es_ambulante",
                table: "lugar");
        }
    }
}
