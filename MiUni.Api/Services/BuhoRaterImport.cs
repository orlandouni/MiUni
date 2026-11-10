using System.Net;
using System.Text.RegularExpressions;

namespace MiUni.Api.Services;

public record PerfilImportado(string Nombre, Guid PerfilId);

public record ResultadoImportacion(IReadOnlyList<PerfilImportado> Perfiles, int SinNombre);

// Lee los perfiles de BuhoRater de una página guardada desde el navegador (.html) o de un
// CSV "nombre,enlace". Solo toma el nombre del profe y el id de su perfil.
public static partial class BuhoRaterImport
{
    [GeneratedRegex(@"<a\b(?<atributos>[^>]*)>(?<texto>.*?)</a\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Enlace();

    [GeneratedRegex(@"\bhref\s*=\s*(?:""(?<v>[^""]*)""|'(?<v>[^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex Href();

    [GeneratedRegex(@"\b(?:aria-label|title)\s*=\s*(?:""(?<v>[^""]*)""|'(?<v>[^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex Etiqueta();

    // Encabezado (h1..h6) o texto alternativo de imagen: en las tarjetas de BuhoRater el enlace
    // al perfil va vacío y el nombre está en el <h3> de la misma tarjeta.
    [GeneratedRegex(@"<h[1-6]\b[^>]*>(?<v>.*?)</h[1-6]\s*>|<img\b[^>]*\balt\s*=\s*(?:""(?<v>[^""]*)""|'(?<v>[^']*)')", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex NombreCercano();

    [GeneratedRegex(@"<(script|style)\b.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptsYEstilos();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Etiquetas();

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex Uuid();

    private static readonly HashSet<string> TextosGenericos =
        new[] { "VER PERFIL", "PERFIL", "VER", "VER MAS", "CALIFICAR", "VER RESENAS", "RESENAS" }
            .Select(t => BuhoRater.Clave(t)!).ToHashSet();

    public static ResultadoImportacion Leer(string contenido, string nombreArchivo)
    {
        var esCsv = nombreArchivo.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            || nombreArchivo.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
        return esCsv ? LeerCsv(contenido) : LeerHtml(contenido);
    }

    public static ResultadoImportacion LeerHtml(string html)
    {
        html = ScriptsYEstilos().Replace(html, " ");
        var perfiles = new List<PerfilImportado>();
        var sinNombre = new HashSet<Guid>();
        var finAnterior = 0;
        foreach (Match enlace in Enlace().Matches(html))
        {
            var antes = html[finAnterior..enlace.Index];
            finAnterior = enlace.Index + enlace.Length;
            var atributos = enlace.Groups["atributos"].Value;
            var href = Href().Match(atributos);
            if (!href.Success || !BuhoRater.TryLeerPerfil(Absoluto(WebUtility.HtmlDecode(href.Groups["v"].Value)), out var id))
                continue;
            var nombre = Nombre(enlace.Groups["texto"].Value);
            if (nombre is null && Etiqueta().Match(atributos) is { Success: true } etiqueta)
                nombre = Nombre(etiqueta.Groups["v"].Value);
            // Lo más cercano antes del enlace y después del enlace anterior: primero un encabezado.
            if (nombre is null)
            {
                var candidatos = NombreCercano().Matches(antes).Cast<Match>().ToList();
                nombre = candidatos.Where(m => m.Value.StartsWith("<h", StringComparison.OrdinalIgnoreCase))
                    .Select(m => Nombre(m.Groups["v"].Value)).LastOrDefault(n => n is not null)
                    ?? candidatos.Select(m => Nombre(m.Groups["v"].Value)).LastOrDefault(n => n is not null);
            }
            if (nombre is null) sinNombre.Add(id);
            else perfiles.Add(new PerfilImportado(nombre, id));
        }
        return Resultado(perfiles, sinNombre);
    }

    public static ResultadoImportacion LeerCsv(string csv)
    {
        var perfiles = new List<PerfilImportado>();
        var sinNombre = new HashSet<Guid>();
        foreach (var linea in csv.Split('\n'))
        {
            var campos = linea.Split([',', ';', '\t']).Select(c => c.Trim().Trim('"').Trim()).ToList();
            var indice = campos.FindIndex(c => BuhoRater.TryLeerPerfil(c, out _));
            if (indice < 0) continue;
            BuhoRater.TryLeerPerfil(campos[indice], out var id);
            var nombre = Nombre(string.Join(' ', campos.Where((_, i) => i != indice)));
            if (nombre is null) sinNombre.Add(id);
            else perfiles.Add(new PerfilImportado(nombre, id));
        }
        return Resultado(perfiles, sinNombre);
    }

    private static ResultadoImportacion Resultado(List<PerfilImportado> perfiles, HashSet<Guid> sinNombre)
    {
        // Un mismo profe puede salir varias veces en la página: se queda la primera.
        var unicos = perfiles.DistinctBy(p => BuhoRater.Clave(p.Nombre)).ToList();
        sinNombre.ExceptWith(unicos.Select(p => p.PerfilId));
        return new ResultadoImportacion(unicos, sinNombre.Count);
    }

    private static string Absoluto(string href) =>
        href.StartsWith('/') ? "https://www.buhorater.com" + href
        : href.StartsWith("perfil", StringComparison.OrdinalIgnoreCase) ? "https://www.buhorater.com/" + href
        : href;

    private static string? Nombre(string fragmento)
    {
        var texto = WebUtility.HtmlDecode(Etiquetas().Replace(fragmento, " "));
        texto = Uuid().Replace(texto, " ");
        texto = string.Join(' ', texto.Split((char[])[' ', '\t', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries));
        if (texto.Length is < 3 or > 120 || !texto.Any(char.IsLetter)) return null;
        if (TextosGenericos.Contains(BuhoRater.Clave(texto)!)) return null;
        return texto;
    }
}
