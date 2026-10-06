using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using Xunit;

namespace MiUni.Api.Tests;

public class AdministracionHttpTests
{
    [RolesHttpFact]
    public async Task ReportesYCrudAdministrativoRespetanRolesPrivacidadYRestricciones()
    {
        using var client = new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable("MIUNI_AUTH_TEST_URL")!) };
        await using var db = new NpgsqlConnection(Environment.GetEnvironmentVariable("MIUNI_ROUTING_TEST_CONNECTION"));
        await db.OpenAsync();
        var prefix = "admin-test-" + Guid.NewGuid().ToString("N");
        var emails = new[] { prefix + "@unison.mx", prefix + "-admin@unison.mx", prefix + "-otro@unison.mx" };
        var password = "Test!" + Guid.NewGuid().ToString("N") + "9aA";
        var categories = new List<Guid>();
        var places = new List<Guid>();
        async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected)
        {
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"Esperado {expected}, recibido {response.StatusCode}: {text.Split('\n')[0]}");
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        void Token(string? value) => client.DefaultRequestHeaders.Authorization = value is null ? null : new AuthenticationHeaderValue("Bearer", value);
        async Task<string> Login(string email) => (await Json(await client.PostAsJsonAsync("/api/Auth/login", new { email, password }), HttpStatusCode.OK)).GetProperty("token").GetString()!;
        try
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/ReporteUsuario")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/Categoria", new { nombre = prefix })).StatusCode);
            foreach (var email in emails)
                Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Auth/register", new { nombre = prefix, email, password })).StatusCode);
            await using (var grant = new NpgsqlCommand("""
                INSERT INTO "AspNetUserRoles" ("UserId","RoleId")
                SELECT u."Id", r."Id" FROM "AspNetUsers" u CROSS JOIN "AspNetRoles" r
                WHERE u."Email"=@email AND r."Name"='Administrador'
                """, db))
            {
                grant.Parameters.AddWithValue("email", emails[1]);
                await grant.ExecuteNonQueryAsync();
            }
            var user = await Login(emails[0]);
            var admin = await Login(emails[1]);
            var other = await Login(emails[2]);
            Token(user);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Lugar/admin")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/ReporteUsuario/resumen")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Categoria", new { nombre = prefix })).StatusCode);
            Token(admin);
            var categoria = await Json(await client.PostAsJsonAsync("/api/Categoria", new { nombre = prefix, icono = "test" }), HttpStatusCode.Created);
            var categoryId = categoria.GetProperty("id").GetGuid(); categories.Add(categoryId);
            var vacia = await Json(await client.PostAsJsonAsync("/api/Categoria", new { nombre = prefix + " vacía" }), HttpStatusCode.Created);
            var emptyId = vacia.GetProperty("id").GetGuid(); categories.Add(emptyId);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Categoria/{emptyId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Categoria/{emptyId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/Categoria/{categoryId}", new { nombre = prefix + " nueva" })).StatusCode);
            var lugarBody = new { nombre = prefix, categoriaId = categoryId, latitud = 29.08, longitud = -110.96, activo = true };
            var lugar = await Json(await client.PostAsJsonAsync("/api/Lugar", lugarBody), HttpStatusCode.Created);
            var placeId = lugar.GetProperty("id").GetGuid(); places.Add(placeId);
            Assert.Equal(29.08, lugar.GetProperty("latitud").GetDouble());
            Assert.Equal(-110.96, lugar.GetProperty("longitud").GetDouble());
            Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/Categoria/{categoryId}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Lugar", new { nombre = prefix, categoriaId = categoryId, latitud = 91, longitud = 0 })).StatusCode);

            Token(user);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/Lugar/{placeId}", lugarBody)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/Lugar/{placeId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/Categoria/{categoryId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/Categoria/{categoryId}", new { nombre = prefix })).StatusCode);
            var resena = await Json(await client.PostAsJsonAsync("/api/Resena", new { lugarId = placeId, calificacion = 3, comentario = "Comentario reportado" }), HttpStatusCode.Created);
            var reviewId = resena.GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/ReporteUsuario", new { motivo = "Nada" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/ReporteUsuario", new { lugarId = placeId, resenaId = reviewId, motivo = "Ambos" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/ReporteUsuario", new { lugarId = Guid.NewGuid(), motivo = "No existe" })).StatusCode);
            var body = new { lugarId = placeId, motivo = "Información incorrecta", estado = "Revisado", usuarioId = Guid.NewGuid() };
            var reporte = await Json(await client.PostAsJsonAsync("/api/ReporteUsuario", body), HttpStatusCode.Created);
            var reportId = reporte.GetProperty("id").GetGuid();
            Assert.Equal("Pendiente", reporte.GetProperty("estado").GetString());
            Assert.Equal(emails[0], reporte.GetProperty("emailUsuario").GetString());
            Assert.Equal(prefix, reporte.GetProperty("nombreLugar").GetString());
            Assert.NotEqual(JsonValueKind.Null, reporte.GetProperty("fechaCreacion").ValueKind);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/ReporteUsuario", body)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/ReporteUsuario/{reportId}", new { motivo = "Motivo corregido" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/ReporteUsuario/{reportId}/estado", new { estado = "Revisado" })).StatusCode);

            Token(other);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/ReporteUsuario/{reportId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/ReporteUsuario/{reportId}", new { motivo = "Ajeno" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/ReporteUsuario/{reportId}")).StatusCode);
            var ownList = await client.GetFromJsonAsync<JsonElement>($"/api/ReporteUsuario?lugarId={placeId}");
            Assert.Empty(ownList.GetProperty("items").EnumerateArray());
            // Otro usuario sí puede reportar el mismo lugar, pero dos peticiones simultáneas no se duplican.
            var parallel = await Task.WhenAll(client.PostAsJsonAsync("/api/ReporteUsuario", body), client.PostAsJsonAsync("/api/ReporteUsuario", body));
            Assert.Single(parallel, r => r.StatusCode == HttpStatusCode.Created);
            Assert.Single(parallel, r => r.StatusCode == HttpStatusCode.Conflict);

            Token(user);
            var reviewReport = await Json(await client.PostAsJsonAsync("/api/ReporteUsuario", new { resenaId = reviewId, motivo = "Reportar comentario" }), HttpStatusCode.Created);
            Assert.Equal("Comentario reportado", reviewReport.GetProperty("comentarioResena").GetString());
            Assert.Equal(placeId, reviewReport.GetProperty("lugarReportadoId").GetGuid());
            Assert.Equal(JsonValueKind.Null, reviewReport.GetProperty("lugarId").ValueKind);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/ReporteUsuario/{reviewReport.GetProperty("id").GetGuid()}")).StatusCode);
            Token(admin);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/ReporteUsuario/{reportId}/estado", new { estado = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/ReporteUsuario/{reportId}/estado", new { estado = "Desconocido" })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync($"/api/ReporteUsuario/{reportId}/estado", new { estado = "Revisado" })).StatusCode);
            var list = await client.GetFromJsonAsync<JsonElement>($"/api/ReporteUsuario?lugarId={placeId}&estado=Revisado");
            Assert.Equal(1, list.GetProperty("total").GetInt32());
            Assert.Equal("Revisado", list.GetProperty("items")[0].GetProperty("estado").GetString());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/ReporteUsuario/resumen")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/ReporteUsuario?pagina=0")).StatusCode);
            Token(user);
            Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/ReporteUsuario/{reportId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/ReporteUsuario/{reportId}", new { motivo = "Ya revisado" })).StatusCode);
            Token(admin);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Lugar/{placeId}")).StatusCode);
            var publicPlaces = await client.GetFromJsonAsync<JsonElement>("/api/Lugar");
            Assert.DoesNotContain(publicPlaces.EnumerateArray(), p => p.GetProperty("id").GetGuid() == placeId);
            var adminPlaces = await client.GetFromJsonAsync<JsonElement>("/api/Lugar/admin?activo=false");
            Assert.Contains(adminPlaces.EnumerateArray(), p => p.GetProperty("id").GetGuid() == placeId);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/Lugar/{placeId}", lugarBody)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/ReporteUsuario/{reportId}")).StatusCode);
            // Restricción XOR también en PostgreSQL para escrituras fuera de la API.
            await using var invalid = new NpgsqlCommand("INSERT INTO reporteusuario(id,usuario_id,motivo,estado) VALUES(gen_random_uuid(),@user,'inválido','Pendiente')", db);
            invalid.Parameters.AddWithValue("user", reporte.GetProperty("usuarioId").GetGuid());
            var exception = await Assert.ThrowsAsync<PostgresException>(() => invalid.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand("""
                DELETE FROM reporteusuario WHERE usuario_id IN (SELECT "Id" FROM "AspNetUsers" WHERE "Email"=ANY(@emails));
                DELETE FROM resena WHERE usuario_id IN (SELECT "Id" FROM "AspNetUsers" WHERE "Email"=ANY(@emails));
                DELETE FROM lugar WHERE id=ANY(@places);
                DELETE FROM categoria WHERE id=ANY(@categories);
                DELETE FROM "AspNetUsers" WHERE "Email"=ANY(@emails);
                """, db);
            cleanup.Parameters.AddWithValue("emails", emails);
            cleanup.Parameters.AddWithValue("places", places.ToArray());
            cleanup.Parameters.AddWithValue("categories", categories.ToArray());
            await cleanup.ExecuteNonQueryAsync();
        }
    }
}
