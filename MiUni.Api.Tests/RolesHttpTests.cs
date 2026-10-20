using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Npgsql;
using Xunit;

namespace MiUni.Api.Tests;

public sealed class RolesHttpFactAttribute : FactAttribute
{
    public RolesHttpFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MIUNI_AUTH_TEST_URL")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MIUNI_ROUTING_TEST_CONNECTION")))
            Skip = "Requiere API actualizada en MIUNI_AUTH_TEST_URL y conexión de pruebas PostgreSQL.";
    }
}

public class RolesHttpTests
{
    [RolesHttpFact]
    public async Task RolesYPropiedadSeVerificanEnHttpYNoSePuedenElegirAlRegistrarse()
    {
        using var client = new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable("MIUNI_AUTH_TEST_URL")!) };
        await using var db = new NpgsqlConnection(Environment.GetEnvironmentVariable("MIUNI_ROUTING_TEST_CONNECTION"));
        await db.OpenAsync();
        var prefix = "roles-test-" + Guid.NewGuid().ToString("N");
        var emailOwner = prefix + "-owner@unison.mx";
        var emailAdmin = prefix + "-admin@unison.mx";
        var emailOther = prefix + "-other@unison.mx";
        var password = "Test!" + Guid.NewGuid().ToString("N") + "9aA";
        var categoria = Guid.NewGuid();
        try
        {
            await using (var category = new NpgsqlCommand("INSERT INTO categoria (id,nombre) VALUES (@id,@name)", db))
            {
                category.Parameters.AddWithValue("id", categoria);
                category.Parameters.AddWithValue("name", prefix);
                await category.ExecuteNonQueryAsync();
            }
            foreach (var email in new[] { emailOwner, emailAdmin, emailOther })
            {
                var response = await client.PostAsJsonAsync("/api/Auth/register", new
                {
                    nombre = prefix, email, password, rol = "Administrador", roles = new[] { "Administrador", "Propietario" }
                });
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            async Task<string> Login(string email)
            {
                var response = await client.PostAsJsonAsync("/api/Auth/login", new { email, password });
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result.GetProperty("token").GetString()!;
            }
            void UseToken(string? value) => client.DefaultRequestHeaders.Authorization = value is null ? null : new AuthenticationHeaderValue("Bearer", value);

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Negocio/solicitudes")).StatusCode);
            var ownerToken = await Login(emailOwner);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(ownerToken);
            Assert.Contains(jwt.Claims, c => c.Value == "Estudiante");
            Assert.DoesNotContain(jwt.Claims, c => c.Value is "Administrador" or "Propietario");
            UseToken(ownerToken);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Negocio/solicitudes")).StatusCode);

            var solicitud = await client.PostAsJsonAsync("/api/Negocio/solicitudes", new
            {
                nombre = prefix, categoriaId = categoria, latitud = 29.08, longitud = -110.96,
                propietarioId = Guid.NewGuid(), estadoSolicitud = "Aprobada", activo = true
            });
            Assert.Equal(HttpStatusCode.Created, solicitud.StatusCode);
            var negocio = await solicitud.Content.ReadFromJsonAsync<JsonElement>();
            var id = negocio.GetProperty("id").GetGuid();
            Assert.Equal("Pendiente", negocio.GetProperty("estadoSolicitud").GetString());
            var publicos = await client.GetFromJsonAsync<JsonElement>("/api/Lugar");
            Assert.DoesNotContain(publicos.EnumerateArray(), p => p.GetProperty("id").GetGuid() == id);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/Negocio/solicitudes/{id}/aprobar", null)).StatusCode);

            // Asignación local exclusivamente a la cuenta desechable de esta prueba.
            async Task Grant(string email, string role)
            {
                await using var grant = new NpgsqlCommand("""
                    INSERT INTO "AspNetUserRoles" ("UserId","RoleId")
                    SELECT u."Id",r."Id" FROM "AspNetUsers" u CROSS JOIN "AspNetRoles" r
                    WHERE u."Email"=@email AND r."Name"=@role ON CONFLICT DO NOTHING
                    """, db);
                grant.Parameters.AddWithValue("email", email);
                grant.Parameters.AddWithValue("role", role);
                await grant.ExecuteNonQueryAsync();
            }
            await Grant(emailAdmin, "Administrador");
            UseToken(await Login(emailAdmin));
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/Negocio/solicitudes/{id}/aprobar", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/Negocio/solicitudes/{id}/aprobar", null)).StatusCode);

            UseToken(ownerToken); // El token anterior recoge los permisos vigentes.
            var me = await client.GetFromJsonAsync<JsonElement>("/api/Auth/me");
            Assert.Contains(me.GetProperty("roles").EnumerateArray(), r => r.GetString() == "Propietario");
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/Negocio/{id}", new { nombre = prefix + " editado" })).StatusCode);
            publicos = await client.GetFromJsonAsync<JsonElement>("/api/Lugar");
            Assert.Contains(publicos.EnumerateArray(), p => p.GetProperty("id").GetGuid() == id);

            await Grant(emailOther, "Propietario");
            UseToken(await Login(emailOther));
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/Negocio/{id}", new { nombre = "Ajeno" })).StatusCode);
            var mios = await client.GetFromJsonAsync<JsonElement>("/api/Negocio/mios");
            Assert.Empty(mios.EnumerateArray());

            UseToken(ownerToken);
            var rejected = await client.PostAsJsonAsync("/api/Negocio/solicitudes", new
                { nombre = prefix + " rechazo", categoriaId = categoria, latitud = 29.08, longitud = -110.96 });
            var rejectedId = (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            UseToken(await Login(emailAdmin));
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/Negocio/solicitudes/{rejectedId}/rechazar", null)).StatusCode);
            UseToken(ownerToken);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/Negocio/{rejectedId}", new { nombre = "Rechazado" })).StatusCode);

            // Revocar un permiso también afecta los tokens todavía vigentes.
            await using (var revoke = new NpgsqlCommand("""
                DELETE FROM "AspNetUserRoles" ur USING "AspNetUsers" u, "AspNetRoles" r
                WHERE ur."UserId"=u."Id" AND ur."RoleId"=r."Id"
                  AND u."Email"=@email AND r."Name"='Propietario'
                """, db))
            {
                revoke.Parameters.AddWithValue("email", emailOwner);
                await revoke.ExecuteNonQueryAsync();
            }
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/Negocio/{id}", new { nombre = "Sin permiso" })).StatusCode);
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand("""
                DELETE FROM lugar WHERE propietario_id IN (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = ANY(@emails));
                DELETE FROM "AspNetUsers" WHERE "Email" = ANY(@emails);
                DELETE FROM categoria WHERE id = @categoria;
                """, db);
            cleanup.Parameters.AddWithValue("emails", new[] { emailOwner, emailAdmin, emailOther });
            cleanup.Parameters.AddWithValue("categoria", categoria);
            await cleanup.ExecuteNonQueryAsync();
        }
    }
}
