using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerRelationToIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CarreraId",
                table: "AspNetUsers",
                column: "CarreraId");

            migrationBuilder.AddForeignKey(
                name: "AspNetUsers_CarreraId_fkey",
                table: "AspNetUsers",
                column: "CarreraId",
                principalTable: "carrera",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "AspNetUsers_CarreraId_fkey",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CarreraId",
                table: "AspNetUsers");
        }
    }
}