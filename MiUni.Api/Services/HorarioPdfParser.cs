using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace MiUni.Api.Services;

public record PalabraPdf(string Texto, double X, double Ancho, double Y, int Pagina)
{
    public double Fin => X + Ancho;
    public double Centro => X + Ancho / 2;
}

public record ClaseImportada(string Materia, string? Profesor, short Dia,
    TimeOnly HoraInicio, TimeOnly HoraFin, string? Salon);

// Lee el "Horario de reinscripción" del portal de alumnos de la UNISON y otros horarios
// en tabla: una fila por materia, una columna por día (Lun, Mar... o LUNES, MARTES...)
// con celdas "07:00-08:00" o "-", y columnas de materia, maestro y aula.
//
// Las columnas se deducen de los huecos verticales entre las palabras de la tabla y se
// nombran con el encabezado más cercano, porque los encabezados vienen centrados y el
// texto de las celdas alineado a la izquierda.
public static partial class HorarioPdfParser
{
    private static readonly Dictionary<string, short> Dias = new()
    {
        ["LUNES"] = 1, ["LUN"] = 1, ["MARTES"] = 2, ["MAR"] = 2, ["MIERCOLES"] = 3, ["MIE"] = 3,
        ["JUEVES"] = 4, ["JUE"] = 4, ["VIERNES"] = 5, ["VIE"] = 5, ["SABADO"] = 6, ["SAB"] = 6,
        ["DOMINGO"] = 7, ["DOM"] = 7,
    };

    private static readonly HashSet<string> EncabezadoMateria = ["MATERIA", "ASIGNATURA", "DESCRIPCION"];
    private static readonly HashSet<string> EncabezadoProfesor = ["MAESTRO", "PROFESOR", "DOCENTE", "CATEDRATICO"];
    private static readonly HashSet<string> EncabezadoSalon = ["AULA", "SALON"];

    private enum Tipo { Otro, Materia, Profesor, Salon, Dia }

    private sealed record Columna(double Inicio, double Fin)
    {
        public Tipo Tipo { get; set; }
        public short Dia { get; set; }
    }

    [GeneratedRegex(@"(\d{1,2})[:.](\d{2})\s*(?:-|–|a)\s*(\d{1,2})[:.](\d{2})")]
    private static partial Regex RangoHoras();

    public static IReadOnlyList<ClaseImportada> Leer(Stream pdf)
    {
        using var documento = PdfDocument.Open(pdf);
        var palabras = documento.GetPages().SelectMany(p => p.GetWords().Select(w => new PalabraPdf(
            w.Text, w.BoundingBox.Left, w.BoundingBox.Width, w.BoundingBox.Bottom, p.Number))).ToList();
        return Leer(palabras);
    }

    public static IReadOnlyList<ClaseImportada> Leer(IEnumerable<PalabraPdf> palabras) => palabras
        .GroupBy(p => p.Pagina).OrderBy(g => g.Key)
        .SelectMany(pagina => LeerPagina(AgruparLineas(pagina)))
        .DistinctBy(c => (c.Materia.ToUpperInvariant(), c.Dia, c.HoraInicio, c.HoraFin))
        .OrderBy(c => c.Dia).ThenBy(c => c.HoraInicio).ToList();

    private static List<List<PalabraPdf>> AgruparLineas(IEnumerable<PalabraPdf> palabras)
    {
        // En PDF la Y crece hacia arriba: se recorre de arriba hacia abajo.
        var lineas = new List<List<PalabraPdf>>();
        foreach (var palabra in palabras.Where(p => !string.IsNullOrWhiteSpace(p.Texto))
                     .OrderByDescending(p => p.Y).ThenBy(p => p.X))
        {
            if (lineas.Count > 0 && Math.Abs(lineas[^1][0].Y - palabra.Y) <= 1.5) lineas[^1].Add(palabra);
            else lineas.Add([palabra]);
        }
        foreach (var linea in lineas) linea.Sort((a, b) => a.X.CompareTo(b.X));
        return lineas;
    }

    private static IEnumerable<ClaseImportada> LeerPagina(List<List<PalabraPdf>> lineas)
    {
        var indice = lineas.FindIndex(l => l.Count(p => Dias.ContainsKey(Normalizar(p.Texto))) >= 3);
        if (indice < 0) return [];
        var encabezado = lineas[indice];
        var tabla = LineasDeTabla(lineas, indice);
        if (tabla.Count == 0) return [];

        var columnas = Columnas(tabla);
        Nombrar(columnas, encabezado);
        if (!columnas.Any(c => c.Tipo == Tipo.Materia) || !columnas.Any(c => c.Tipo == Tipo.Dia)) return [];

        var primeraMateria = columnas.First(c => c.Tipo == Tipo.Materia).Inicio;
        var filas = new List<Dictionary<Columna, string>>();
        foreach (var linea in tabla)
        {
            var celdas = Celdas(linea, columnas);
            var tieneHoras = celdas.Any(c => c.Key.Tipo == Tipo.Dia && RangoHoras().IsMatch(c.Value));
            // Prog./Clave (lo que va antes de la materia) solo aparece en la primera línea de cada fila.
            var empiezaFila = celdas.Keys.Any(c => c.Fin < primeraMateria && c.Tipo == Tipo.Otro);
            var anterior = filas.LastOrDefault();
            var anteriorTieneHoras = anterior?.Any(c => c.Key.Tipo == Tipo.Dia && RangoHoras().IsMatch(c.Value)) ?? false;

            if (anterior is null || empiezaFila || tieneHoras && anteriorTieneHoras && !columnas.Any(c => c.Fin < primeraMateria))
                filas.Add(celdas);
            else
                // Celdas de varias líneas: el nombre partido o las horas centradas a media fila.
                foreach (var (columna, texto) in celdas)
                    anterior[columna] = anterior.TryGetValue(columna, out var previo) ? $"{previo} {texto}" : texto;
        }

        return filas.SelectMany(Clases);
    }

    // Líneas bajo el encabezado hasta el primer salto grande (p. ej. la sección de notas).
    private static List<List<PalabraPdf>> LineasDeTabla(List<List<PalabraPdf>> lineas, int encabezado)
    {
        var tabla = new List<List<PalabraPdf>>();
        var anteriorY = lineas[encabezado][0].Y;
        foreach (var linea in lineas.Skip(encabezado + 1))
        {
            if (anteriorY - linea[0].Y > 25) break;
            tabla.Add(linea);
            anteriorY = linea[0].Y;
        }
        return tabla;
    }

    private static List<Columna> Columnas(List<List<PalabraPdf>> tabla)
    {
        // El espacio entre palabras de una misma celda es el hueco más común en la tabla.
        var huecos = tabla.SelectMany(l => l.Zip(l.Skip(1), (a, b) => b.X - a.Fin)).Where(h => h is > 0 and < 8).Order().ToList();
        var espacio = huecos.Count > 0 ? huecos[huecos.Count / 2] : 2;
        var separacion = Math.Max(espacio * 1.6, 2.5);

        var columnas = new List<Columna>();
        foreach (var palabra in tabla.SelectMany(l => l).OrderBy(p => p.X))
        {
            if (columnas.Count > 0 && palabra.X - columnas[^1].Fin < separacion)
                columnas[^1] = columnas[^1] with { Fin = Math.Max(columnas[^1].Fin, palabra.Fin) };
            else columnas.Add(new Columna(palabra.X, palabra.Fin));
        }
        return columnas;
    }

    private static void Nombrar(List<Columna> columnas, List<PalabraPdf> encabezado)
    {
        foreach (var palabra in encabezado)
        {
            var texto = Normalizar(palabra.Texto);
            var tipo = Dias.ContainsKey(texto) ? Tipo.Dia
                : EncabezadoMateria.Contains(texto) ? Tipo.Materia
                : EncabezadoProfesor.Contains(texto) ? Tipo.Profesor
                : EncabezadoSalon.Contains(texto) ? Tipo.Salon : Tipo.Otro;
            if (tipo == Tipo.Otro) continue;
            var columna = columnas.MinBy(c => Distancia(c, palabra.Centro))!;
            if (columna.Tipo != Tipo.Otro) continue;
            columna.Tipo = tipo;
            if (tipo == Tipo.Dia) columna.Dia = Dias[texto];
        }
    }

    private static double Distancia(Columna c, double x) => x < c.Inicio ? c.Inicio - x : x > c.Fin ? x - c.Fin : 0;

    private static Dictionary<Columna, string> Celdas(List<PalabraPdf> linea, List<Columna> columnas)
    {
        var celdas = new Dictionary<Columna, string>();
        foreach (var palabra in linea)
        {
            var columna = columnas.MinBy(c => Distancia(c, palabra.Centro))!;
            celdas[columna] = celdas.TryGetValue(columna, out var texto) ? $"{texto} {palabra.Texto}" : palabra.Texto;
        }
        return celdas;
    }

    private static IEnumerable<ClaseImportada> Clases(Dictionary<Columna, string> fila)
    {
        var materia = Texto(fila, Tipo.Materia);
        if (materia is null) yield break;
        var profesor = Texto(fila, Tipo.Profesor);
        var aula = Texto(fila, Tipo.Salon);
        foreach (var (columna, texto) in fila.Where(c => c.Key.Tipo == Tipo.Dia))
        {
            var m = RangoHoras().Match(texto);
            if (!m.Success) continue;
            int Num(int i) => int.Parse(m.Groups[i].Value, CultureInfo.InvariantCulture);
            if (Num(1) > 23 || Num(3) > 23 || Num(2) > 59 || Num(4) > 59) continue;
            var (inicio, fin) = (new TimeOnly(Num(1), Num(2)), new TimeOnly(Num(3), Num(4)));
            if (fin <= inicio) continue;
            var resto = Limpiar(texto.Remove(m.Index, m.Length).Trim(' ', '-', ',', '/'));
            yield return new ClaseImportada(Recortar(materia, 120)!, Recortar(profesor, 120), columna.Dia,
                inicio, fin, Recortar(resto ?? aula, 60));
        }
    }

    private static string? Texto(Dictionary<Columna, string> fila, Tipo tipo) =>
        Limpiar(string.Join(' ', fila.Where(c => c.Key.Tipo == tipo).OrderBy(c => c.Key.Inicio).Select(c => c.Value)));

    private static string? Limpiar(string? texto)
    {
        var limpio = string.Join(' ', (texto ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length == 0 || limpio == "-" ? null : limpio;
    }

    private static string? Recortar(string? texto, int maximo) =>
        texto is null ? null : texto.Length <= maximo ? texto : texto[..maximo].TrimEnd();

    private static string Normalizar(string texto)
    {
        var sinAcentos = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetter(c))
                sinAcentos.Append(char.ToUpperInvariant(c));
        return sinAcentos.ToString();
    }
}
