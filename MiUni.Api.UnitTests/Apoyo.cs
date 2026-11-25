using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Models;

namespace MiUni.Api.UnitTests;

// Utilidades compartidas por las pruebas.
internal static class Apoyo
{
    // Base de datos en memoria nueva para cada prueba (no necesita PostgreSQL).
    public static MiUniDbContext BaseEnMemoria() => new(new DbContextOptionsBuilder<MiUniDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // Ejecuta las mismas validaciones [Required], [Range], etc. que aplica ASP.NET Core.
    public static List<ValidationResult> Validar(object modelo)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true);
        return errores;
    }

    public static string Json(object? valor) => JsonSerializer.Serialize(valor,
        new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
}

// Tipo de caso que cubre cada prueba (se ve en el explorador de pruebas y sirve con --filter).
internal static class Caso
{
    public const string Normal = "Caso normal";
    public const string Error = "Caso de error";
    public const string Limite = "Caso límite";
    public const string Negocio = "Regla de negocio";
    // Pruebas que fallan a propósito porque exhiben un defecto real de la API.
    public const string Defecto = "DefectoConocido";
}
