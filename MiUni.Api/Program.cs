using Microsoft.EntityFrameworkCore;
using MiUni.Api.Enums;
using MiUni.Api.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MiUniDb");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<RolUsuario>("rol_usuario");
dataSourceBuilder.MapEnum<EstadoReporte>("estado_reporte");
dataSourceBuilder.UseNetTopologySuite();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<MiUniDbContext>(options =>
    options.UseNpgsql(dataSource, o => o.UseNetTopologySuite())
);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();