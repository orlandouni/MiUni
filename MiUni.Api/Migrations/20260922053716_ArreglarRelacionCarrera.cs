using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class ArreglarRelacionCarrera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_carrera_CarreraId1",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CarreraId1",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CarreraId1",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CarreraId1",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CarreraId1",
                table: "AspNetUsers",
                column: "CarreraId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_carrera_CarreraId1",
                table: "AspNetUsers",
                column: "CarreraId1",
                principalTable: "carrera",
                principalColumn: "id");
        }
    }
}
