using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MiUni.Api.Enums;
using MiUni.Api.Models;
using Npgsql;

public class MiUniDbContextFactory : IDesignTimeDbContextFactory<MiUniDbContext>
{
    public MiUniDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("MiUniDb");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<RolUsuario>("rol_usuario");
        dataSourceBuilder.MapEnum<EstadoReporte>("estado_reporte");
        dataSourceBuilder.UseNetTopologySuite();
        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<MiUniDbContext>();
        optionsBuilder.UseNpgsql(dataSource, o => o.UseNetTopologySuite());

        return new MiUniDbContext(optionsBuilder.Options);
    }
}