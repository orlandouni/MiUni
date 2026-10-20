using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Enums;
using MiUni.Api.Models;

namespace MiUni.Api.Identity;

public static class DevelopmentAdminSeeder
{
    // Identifica exclusivamente la cuenta administrada por este seed.
    private static readonly Guid AdminId = Guid.Parse("71e598bc-305a-4d44-97c3-3929722dd741");

    public static async Task SeedAsync(IServiceProvider services, IHostEnvironment environment,
        IConfiguration configuration, CancellationToken ct = default)
    {
        // Las credenciales compartidas son sólo para las bases de desarrollo.
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DevelopmentAdmin:Enabled")) return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MiUniDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var email = configuration["DevelopmentAdmin:Email"]?.Trim();
        var password = configuration["DevelopmentAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Configura DevelopmentAdmin:Email y DevelopmentAdmin:Password para ejecutar el seed.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Evita duplicados si dos instancias arrancan contra la misma base local.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(724691504)", ct);
        foreach (var role in new[] { AppRoles.Usuario, AppRoles.Propietario, AppRoles.Administrador })
            if (!await roles.RoleExistsAsync(role))
                Ensure(await roles.CreateAsync(new IdentityRole<Guid>(role)), "crear roles");

        var account = await users.FindByIdAsync(AdminId.ToString());
        if (account is null)
        {
            // Nunca convierte en administrador una cuenta ajena que use ese correo.
            if (await users.FindByEmailAsync(email) is not null || await users.FindByNameAsync(email) is not null)
                throw new InvalidOperationException("El correo del seed ya pertenece a otra cuenta. Configura otro DevelopmentAdmin:Email.");
            account = new ApplicationUser
            {
                Id = AdminId, UserName = email, Email = email,
                Nombre = "Administrador de desarrollo", FechaRegistro = DateTime.UtcNow,
                Rol = RolUsuario.Administrador
            };
            Ensure(await users.CreateAsync(account, password), "crear el administrador de desarrollo");
        }

        foreach (var role in new[] { AppRoles.Usuario, AppRoles.Administrador })
            if (!await users.IsInRoleAsync(account, role))
                Ensure(await users.AddToRoleAsync(account, role), "asignar permisos al administrador de desarrollo");

        // No modifica correo, perfil ni contraseña de una cuenta ya sembrada.
        await transaction.CommitAsync(ct);
    }

    private static void Ensure(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"No se pudo {operation}: {string.Join(", ", result.Errors.Select(e => e.Code))}");
    }
}
