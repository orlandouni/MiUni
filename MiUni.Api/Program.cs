using Microsoft.EntityFrameworkCore;
using MiUni.Api.Enums;
using MiUni.Api.Models;
using Npgsql;
using Microsoft.AspNetCore.Identity;
using MiUni.Api.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using MiUni.Api.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MiUniDb");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
var enumNameTranslator = new Npgsql.NameTranslation.NpgsqlNullNameTranslator();
dataSourceBuilder.MapEnum<RolUsuario>("rol_usuario");
dataSourceBuilder.MapEnum<EstadoReporte>("estado_reporte", enumNameTranslator);
dataSourceBuilder.UseNetTopologySuite();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<IRutaService, RutaService>();
builder.Services.AddScoped<ICorreoService, SmtpCorreoService>();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<MiUniDbContext>(options =>
    options.UseNpgsql(dataSource, o => o.UseNetTopologySuite()
        .MapEnum<EstadoReporte>("estado_reporte", nameTranslator: enumNameTranslator))
);

builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<MiUniDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddDataProtection();
// Los enlaces de verificación de correo vencen en 24 horas.
builder.Services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(24));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Lee los permisos vigentes: una aprobación o revocación no exige esperar
        // a que venza un JWT emitido anteriormente.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = id is null ? null : await manager.FindByIdAsync(id);
                if (user is null || await manager.IsLockedOutAsync(user)) { context.Fail("Cuenta no disponible."); return; }
                var identity = (ClaimsIdentity)context.Principal!.Identity!;
                foreach (var claim in identity.FindAll(ClaimTypes.Role).ToList()) identity.RemoveClaim(claim);
                foreach (var role in await manager.GetRolesAsync(user)) identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RoleClaimType = ClaimTypes.Role,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<IRagClient, RagClient>(client =>
{
    var baseUrl = builder.Configuration["Rag:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce el JWT obtenido del endpoint /api/Auth/login."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", document),
                new List<string>()
            }
        });
});

var app = builder.Build();

// Operación local, sin endpoint público ni contraseñas predeterminadas.
var adminEmail = builder.Configuration["asignar-admin"];
if (!string.IsNullOrWhiteSpace(adminEmail))
{
    using var scope = app.Services.CreateScope();
    var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var account = await manager.FindByEmailAsync(adminEmail.Trim());
    if (account is null) throw new InvalidOperationException("Registra la cuenta antes de asignarle el rol de administrador.");
    if (!await manager.IsInRoleAsync(account, AppRoles.Administrador))
    {
        var result = await manager.AddToRoleAsync(account, AppRoles.Administrador);
        if (!result.Succeeded) throw new InvalidOperationException("No se pudo asignar el rol de administrador.");
    }
    Console.WriteLine("Rol de administrador asignado a la cuenta indicada.");
    return;
}

// Importa la red peatonal de OpenStreetMap a public.camino y termina, sin levantar la API.
var areaOsm = builder.Configuration["importar-caminos-osm"];
if (!string.IsNullOrWhiteSpace(areaOsm))
{
    var area = AreaOsm.Parse(areaOsm);
    var overpassUrl = builder.Configuration["osm-overpass-url"] ?? "https://overpass-api.de/api/interpreter";
    var incluirPrivados = builder.Configuration.GetValue<bool>("osm-incluir-privados");
    using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("MiUni-importador-caminos/1.0");
    Console.WriteLine($"Descargando caminos de OpenStreetMap ({area.Sur}, {area.Oeste}) - ({area.Norte}, {area.Este})...");
    using var osm = await OsmCaminos.DescargarAsync(http, area, overpassUrl);
    var segmentos = OsmCaminos.Segmentar(osm.RootElement, incluirPrivados);
    var guardados = await OsmCaminos.GuardarAsync(dataSource, segmentos.Tramos);
    Console.WriteLine($"Importados {guardados} tramos de {segmentos.ViasUsadas} vías OSM. " +
        $"Descartados {segmentos.TramosAislados} tramos aislados de la red principal.");
    var manuales = await OsmCaminos.ContarManualesAsync(dataSource);
    if (manuales > 0)
        Console.WriteLine($"Aviso: quedan {manuales} caminos manuales; sólo se conectan si sus extremos coinciden con la red OSM. " +
            "Para quitarlos: DELETE FROM public.camino WHERE osm_way_id IS NULL;");
    Console.WriteLine("Datos © colaboradores de OpenStreetMap (ODbL).");
    return;
}

if (app.Environment.IsDevelopment())
{
    await DevelopmentAdminSeeder.SeedAsync(app.Services, app.Environment, app.Configuration);
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
