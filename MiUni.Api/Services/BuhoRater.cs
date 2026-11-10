namespace MiUni.Api.Services;

// BuhoRater (calificaciones de maestros de la UNISON) no ofrece API pública y bloquea
// el acceso automatizado, así que MiUni solo enlaza: no descarga ni guarda sus datos.
public static class BuhoRater
{
    public const string SitioUrl = "https://www.buhorater.com/";

    // Búsqueda del profesor dentro de buhorater.com. Si BuhoRater publica una URL de
    // búsqueda propia, este es el único lugar que hay que cambiar.
    public static string? UrlBusqueda(string? profesor)
    {
        var nombre = string.Join(' ', (profesor ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (nombre.Length == 0) return null;
        var consulta = Uri.EscapeDataString($"site:buhorater.com {nombre}");
        return $"https://www.google.com/search?q={consulta}";
    }
}
