using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MiUni.Api.Models;

#nullable disable

namespace MiUni.Api.Migrations;

[DbContext(typeof(MiUniDbContext))]
[Migration("20261116120000_DocumentTemporalMetadata")]
public partial class DocumentTemporalMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "temporal_status", table: "documentofuente", type: "text",
            nullable: false, defaultValue: "desconocido");
        migrationBuilder.AddColumn<int>(name: "document_year", table: "documentofuente", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "academic_period", table: "documentofuente", type: "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "effective_from", table: "documentofuente", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "effective_to", table: "documentofuente", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "last_verified_at", table: "documentofuente", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "superseded_by", table: "documentofuente", type: "uuid", nullable: true);
        migrationBuilder.AddCheckConstraint(name: "ck_documentofuente_temporal_status", table: "documentofuente",
            sql: "temporal_status IN ('vigente', 'reemplazado', 'historico', 'desconocido')");
        migrationBuilder.AddCheckConstraint(name: "ck_documentofuente_document_year", table: "documentofuente",
            sql: "document_year IS NULL OR document_year BETWEEN 1000 AND 9999");
        migrationBuilder.CreateIndex(name: "idx_documentofuente_temporal_status", table: "documentofuente", column: "temporal_status");
        migrationBuilder.CreateIndex(name: "idx_documentofuente_academic_period", table: "documentofuente", column: "academic_period");
        migrationBuilder.CreateIndex(name: "idx_documentofuente_superseded_by", table: "documentofuente", column: "superseded_by");
        migrationBuilder.AddForeignKey(name: "documentofuente_superseded_by_fkey", table: "documentofuente",
            column: "superseded_by", principalTable: "documentofuente", principalColumn: "id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "documentofuente_superseded_by_fkey", table: "documentofuente");
        migrationBuilder.DropIndex(name: "idx_documentofuente_temporal_status", table: "documentofuente");
        migrationBuilder.DropIndex(name: "idx_documentofuente_academic_period", table: "documentofuente");
        migrationBuilder.DropIndex(name: "idx_documentofuente_superseded_by", table: "documentofuente");
        migrationBuilder.DropCheckConstraint(name: "ck_documentofuente_temporal_status", table: "documentofuente");
        migrationBuilder.DropCheckConstraint(name: "ck_documentofuente_document_year", table: "documentofuente");
        foreach (var column in new[] { "temporal_status", "document_year", "academic_period", "effective_from", "effective_to", "last_verified_at", "superseded_by" })
            migrationBuilder.DropColumn(name: column, table: "documentofuente");
    }
}
