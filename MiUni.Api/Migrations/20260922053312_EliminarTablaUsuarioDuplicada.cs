using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class EliminarTablaUsuarioDuplicada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "favorito_usuario_id_fkey",
                table: "favorito");

            migrationBuilder.DropForeignKey(
                name: "historialchat_usuario_id_fkey",
                table: "historialchat");

            migrationBuilder.DropForeignKey(
                name: "reporteusuario_usuario_id_fkey",
                table: "reporteusuario");

            migrationBuilder.DropForeignKey(
                name: "resena_usuario_id_fkey",
                table: "resena");

            migrationBuilder.DropTable(
                name: "usuario");

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

            migrationBuilder.AddForeignKey(
                name: "favorito_usuario_id_fkey",
                table: "favorito",
                column: "usuario_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "historialchat_usuario_id_fkey",
                table: "historialchat",
                column: "usuario_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "reporteusuario_usuario_id_fkey",
                table: "reporteusuario",
                column: "usuario_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "resena_usuario_id_fkey",
                table: "resena",
                column: "usuario_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_carrera_CarreraId1",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "favorito_usuario_id_fkey",
                table: "favorito");

            migrationBuilder.DropForeignKey(
                name: "historialchat_usuario_id_fkey",
                table: "historialchat");

            migrationBuilder.DropForeignKey(
                name: "reporteusuario_usuario_id_fkey",
                table: "reporteusuario");

            migrationBuilder.DropForeignKey(
                name: "resena_usuario_id_fkey",
                table: "resena");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CarreraId1",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CarreraId1",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    carrera_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correo_institucional = table.Column<string>(type: "character varying", nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nombre = table.Column<string>(type: "character varying", nullable: false),
                    password_hash = table.Column<string>(type: "character varying", nullable: false),
                    Rol = table.Column<int>(type: "integer", nullable: false),
                    semestre = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("usuario_pkey", x => x.id);
                    table.ForeignKey(
                        name: "usuario_carrera_id_fkey",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_usuario_carrera",
                table: "usuario",
                column: "carrera_id");

            migrationBuilder.CreateIndex(
                name: "usuario_correo_institucional_key",
                table: "usuario",
                column: "correo_institucional",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "favorito_usuario_id_fkey",
                table: "favorito",
                column: "usuario_id",
                principalTable: "usuario",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "historialchat_usuario_id_fkey",
                table: "historialchat",
                column: "usuario_id",
                principalTable: "usuario",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "reporteusuario_usuario_id_fkey",
                table: "reporteusuario",
                column: "usuario_id",
                principalTable: "usuario",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "resena_usuario_id_fkey",
                table: "resena",
                column: "usuario_id",
                principalTable: "usuario",
                principalColumn: "id");
        }
    }
}
