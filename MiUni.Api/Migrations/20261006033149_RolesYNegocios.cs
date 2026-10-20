using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class RolesYNegocios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
                VALUES (gen_random_uuid(), 'Estudiante', 'ESTUDIANTE', gen_random_uuid()::text),
                       (gen_random_uuid(), 'Propietario', 'PROPIETARIO', gen_random_uuid()::text),
                       (gen_random_uuid(), 'Administrador', 'ADMINISTRADOR', gen_random_uuid()::text)
                ON CONFLICT ("NormalizedName") DO NOTHING;

                INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
                SELECT u."Id", r."Id" FROM "AspNetUsers" u
                CROSS JOIN "AspNetRoles" r WHERE r."NormalizedName" = 'ESTUDIANTE'
                ON CONFLICT DO NOTHING;

                INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
                SELECT u."Id", r."Id" FROM "AspNetUsers" u
                CROSS JOIN "AspNetRoles" r
                WHERE u."Rol" = 'Administrador' AND r."NormalizedName" = 'ADMINISTRADOR'
                ON CONFLICT DO NOTHING;
                """);
            migrationBuilder.AddColumn<string>(
                name: "estado_solicitud",
                table: "lugar",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "propietario_id",
                table: "lugar",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lugar_propietario_id",
                table: "lugar",
                column: "propietario_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lugar_AspNetUsers_propietario_id",
                table: "lugar",
                column: "propietario_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lugar_AspNetUsers_propietario_id",
                table: "lugar");

            migrationBuilder.DropIndex(
                name: "IX_lugar_propietario_id",
                table: "lugar");

            migrationBuilder.DropColumn(
                name: "estado_solicitud",
                table: "lugar");

            migrationBuilder.DropColumn(
                name: "propietario_id",
                table: "lugar");
        }
    }
}
