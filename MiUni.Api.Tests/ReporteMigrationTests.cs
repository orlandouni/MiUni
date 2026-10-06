using Microsoft.EntityFrameworkCore.Migrations.Operations;
using MiUni.Api.Migrations;
using Npgsql;
using Xunit;

namespace MiUni.Api.Tests;

public class ReporteMigrationTests
{
    [RoutingFact]
    public async Task ConservaEstadosTantoDelEsquemaInicialComoDelOriginal()
    {
        await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("MIUNI_ROUTING_TEST_CONNECTION"));
        await connection.OpenAsync();
        foreach (var original in new[] { false, true })
        {
            // El esquema desechable y sus filas desaparecen al revertir esta transacción.
            await using var transaction = await connection.BeginTransactionAsync();
            var schema = "test_migracion_" + Guid.NewGuid().ToString("N");
            var column = original ? "estado public.estado_reporte NOT NULL" : "\"Estado\" integer NOT NULL";
            var values = original ? "(1,'Pendiente'),(2,'Revisado'),(3,'Descartado')" : "(1,0),(2,1),(3,2)";
            await using (var setup = new NpgsqlCommand($"CREATE SCHEMA {schema}; CREATE TABLE {schema}.reporteusuario(id integer, {column}); INSERT INTO {schema}.reporteusuario VALUES {values}", connection, transaction))
                await setup.ExecuteNonQueryAsync();
            var sql = new ReportesYAdministracion().UpOperations.OfType<SqlOperation>().First().Sql
                .Replace("public.", schema + ".").Replace("table_schema='public'", $"table_schema='{schema}'");
            await using (var migrate = new NpgsqlCommand(sql, connection, transaction))
                await migrate.ExecuteNonQueryAsync();
            await using var check = new NpgsqlCommand($"SELECT string_agg(estado::text, ',' ORDER BY id) FROM {schema}.reporteusuario", connection, transaction);
            Assert.Equal("Pendiente,Revisado,Descartado", await check.ExecuteScalarAsync());
            await transaction.RollbackAsync();
        }
    }
}
