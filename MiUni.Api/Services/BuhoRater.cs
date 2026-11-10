using System.Globalization;
using System.Text;
using System.Web;

namespace MiUni.Api.Services;

// BuhoRater (calificaciones de maestros de la UNISON) no ofrece API pública y bloquea
// el acceso automatizado, así que MiUni solo enlaza: no descarga ni guarda sus datos.
// El perfil de cada profe (https://www.buhorater.com/perfil?id=<uuid>) lo pegan los
// estudiantes; mientras no exista, el botón abre una búsqueda del nombre.
public static class BuhoRater
{
    public const string SitioUrl = "https://www.buhorater.com/";

    public static string UrlPerfil(Guid perfilId) => $"https://www.buhorater.com/perfil?id={perfilId:D}";

    public static string? UrlBusqueda(string? profesor)
    {
        var nombre = string.Join(' ', (profesor ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (nombre.Length == 0) return null;
        var consulta = Uri.EscapeDataString($"site:buhorater.com {nombre}");
        return $"https://www.google.com/search?q={consulta}";
    }

    // El perfil guardado o, si no hay, la búsqueda del nombre.
    public static string? Url(string? profesor, IReadOnlyDictionary<string, Guid> perfiles) =>
        Clave(profesor) is { } clave && perfiles.TryGetValue(clave, out var id) ? UrlPerfil(id) : UrlBusqueda(profesor);

    public static bool TienePerfil(string? profesor, IReadOnlyDictionary<string, Guid> perfiles) =>
        Clave(profesor) is { } clave && perfiles.ContainsKey(clave);

    // Solo se acepta un enlace de perfil de buhorater.com (o el id suelto): MiUni nunca
    // guarda la URL que pega el usuario, solo el id, y arma el enlace por su cuenta.
    public static bool TryLeerPerfil(string? enlace, out Guid perfilId)
    {
        perfilId = Guid.Empty;
        var texto = (enlace ?? "").Trim();
        if (Guid.TryParseExact(texto, "D", out perfilId)) return true;
        if (!texto.Contains("://")) texto = "https://" + texto;
        if (!Uri.TryCreate(texto, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme is not ("https" or "http")) return false;
        if (!uri.Host.Equals("www.buhorater.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("buhorater.com", StringComparison.OrdinalIgnoreCase)) return false;
        if (!uri.AbsolutePath.TrimEnd('/').Equals("/perfil", StringComparison.OrdinalIgnoreCase)) return false;
        return Guid.TryParseExact(HttpUtility.ParseQueryString(uri.Query)["id"] ?? "", "D", out perfilId)
            && perfilId != Guid.Empty;
    }

    public static string? Clave(string? profesor)
    {
        var sinAcentos = new StringBuilder();
        foreach (var c in (profesor ?? "").Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sinAcentos.Append(char.ToUpperInvariant(c));
        var clave = string.Join(' ', sinAcentos.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return clave.Length == 0 ? null : clave;
    }
}
