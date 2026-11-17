using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using MiUni.Api.Models;
using Xunit;

namespace MiUni.Api.Tests;

public class RagTemporalSchemaTests
{
    private static MiUniDbContext CreateContext() => new(new DbContextOptionsBuilder<MiUniDbContext>()
        .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused", o => o.UseNetTopologySuite())
        .Options);

    [Fact]
    public void DocumentMetadataHasCanonicalColumnsConstraintsAndIndexedJoin()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var document = model.FindEntityType(typeof(Documentofuente))!;
        var table = StoreObjectIdentifier.Table("documentofuente", null);
        var columns = new Dictionary<string, (string Column, string Type, bool Nullable)>
        {
            ["TemporalStatus"] = ("temporal_status", "text", false),
            ["DocumentYear"] = ("document_year", "integer", true),
            ["AcademicPeriod"] = ("academic_period", "text", true),
            ["EffectiveFrom"] = ("effective_from", "timestamp with time zone", true),
            ["EffectiveTo"] = ("effective_to", "timestamp with time zone", true),
            ["LastVerifiedAt"] = ("last_verified_at", "timestamp with time zone", true),
            ["SupersededBy"] = ("superseded_by", "uuid", true)
        };
        foreach (var (name, expected) in columns)
        {
            var property = document.FindProperty(name);
            Assert.NotNull(property);
            Assert.Equal(expected.Column, property.GetColumnName(table));
            Assert.Equal(expected.Type, property.GetColumnType());
            Assert.Equal(expected.Nullable, property.IsNullable);
        }
        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot!.Model
            .FindEntityType(typeof(Documentofuente).FullName!)!;
        foreach (var name in columns.Keys)
        {
            var expected = document.FindProperty(name)!;
            var actual = snapshot.FindProperty(name)!;
            Assert.Equal(expected.GetColumnName(table), actual.GetColumnName(table));
            Assert.Equal(expected.GetColumnType(), actual.GetColumnType());
            Assert.Equal(expected.IsNullable, actual.IsNullable);
        }
        Assert.Equal("desconocido", snapshot.FindProperty("TemporalStatus")!.GetDefaultValue());
        Assert.Equal("desconocido", document.FindProperty("TemporalStatus")!.GetDefaultValue());
        Assert.Contains(document.GetCheckConstraints(), c => c.Name == "ck_documentofuente_temporal_status"
            && c.Sql == "temporal_status IN ('vigente', 'reemplazado', 'historico', 'desconocido')");
        Assert.Contains(document.GetCheckConstraints(), c => c.Name == "ck_documentofuente_document_year"
            && c.Sql == "document_year IS NULL OR document_year BETWEEN 1000 AND 9999");
        foreach (var property in new[] { "TemporalStatus", "AcademicPeriod", "SupersededBy" })
            Assert.Contains(document.GetIndexes(), i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { property }));
        var replacement = Assert.Single(document.GetForeignKeys(), f => f.Properties.Any(p => p.Name == "SupersededBy"));
        Assert.Equal(document, replacement.PrincipalEntityType);
        Assert.Equal(DeleteBehavior.Restrict, replacement.DeleteBehavior);
        var chunk = model.FindEntityType(typeof(Ragchunk))!;
        Assert.Contains(chunk.GetIndexes(), i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "DocumentoFuenteId" }));
        Assert.Equal(DeleteBehavior.Cascade, Assert.Single(chunk.GetForeignKeys()).DeleteBehavior);
    }

    [Fact]
    public void RegisteredMigrationOnlyAddsDocumentMetadataWithoutRewritingContent()
    {
        using var context = CreateContext();
        var assembly = context.GetService<IMigrationsAssembly>();
        const string id = "20261116120000_DocumentTemporalMetadata";
        Assert.True(assembly.Migrations.TryGetValue(id, out var type));
        var migration = assembly.CreateMigration(type!, context.Database.ProviderName!);
        var additions = migration.UpOperations.OfType<AddColumnOperation>().ToArray();
        Assert.Equal(7, additions.Length);
        Assert.All(additions, a => Assert.Equal("documentofuente", a.Table));
        Assert.Equal("desconocido", Assert.Single(additions, a => a.Name == "temporal_status").DefaultValue);
        Assert.All(migration.UpOperations, operation => Assert.True(operation is AddColumnOperation
            or AddCheckConstraintOperation or CreateIndexOperation or AddForeignKeyOperation));
        var fk = Assert.Single(migration.UpOperations.OfType<AddForeignKeyOperation>());
        Assert.Equal(ReferentialAction.Restrict, fk.OnDelete);
        var model = context.GetService<IDesignTimeModel>().Model;
        var sql = string.Join("\n", context.GetService<IMigrationsSqlGenerator>()
            .Generate(migration.UpOperations, model).Select(c => c.CommandText));
        var registeredScript = context.GetService<IMigrator>().GenerateScript(
            "20261110162818_OrdenarClaveProfesorBuhoRater", id);
        Assert.Contains("ADD", registeredScript);
        Assert.DoesNotContain("DELETE FROM", registeredScript);
        Assert.DoesNotContain("UPDATE", registeredScript);
        Assert.Contains("ADD", sql);
        Assert.DoesNotContain("DELETE FROM", sql);
        Assert.DoesNotContain("UPDATE", sql);
        Assert.DoesNotContain("rag_chunk", sql);
        Assert.Equal(7, migration.DownOperations.OfType<DropColumnOperation>().Count());
        Assert.All(migration.DownOperations.OfType<DropColumnOperation>(), c => Assert.Equal("documentofuente", c.Table));
    }
}
