using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MiUni.Api.Identity;
using Xunit;

namespace MiUni.Api.Tests;

public class DevelopmentAdminSeederTests
{
    [Theory]
    [InlineData("Production", true)]
    [InlineData("Staging", true)]
    [InlineData("Development", false)]
    public async Task NoAccedeALaBaseFueraDeDesarrolloOHabilitacion(string environment, bool enabled)
    {
        // No hay DbContext ni UserManager: si intenta acceder a la base, falla.
        using var services = new ServiceCollection().BuildServiceProvider();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["DevelopmentAdmin:Enabled"] = enabled.ToString() }).Build();
        await DevelopmentAdminSeeder.SeedAsync(services, new TestEnvironment(environment), config);
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
