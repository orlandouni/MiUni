using System.Globalization;
using System.Text;

namespace MiUni.Api.Services;

public sealed record ChatPlace(string Nombre, string? Categoria);

public static class ChatPlaceRouter
{
    private static readonly string[] IntentPrefixes =
    [
        "donde", "donde esta", "donde se encuentra", "ubicacion", "como llego", "como llegar",
        "llevarme a", "ir a", "llegar a"
    ];

    private static readonly string[] DocumentaryTerms =
    [
        "hora", "horas", "horario", "horarios", "abre", "cierra", "apertura", "cierre",
        "requisito", "requisitos", "tramite", "tramites", "reglamento", "reglamentos"
    ];

    public static string? TryBuildAnswer(string question, IEnumerable<ChatPlace> places)
    {
        var normalizedQuestion = Normalize(question);
        if (normalizedQuestion is "que lugares hay" or "que lugares hay en el campus"
            or "que lugares hay en campus" or "muestrame los lugares" or "lista de lugares")
        {
            var names = places.Where(place => !string.IsNullOrWhiteSpace(place.Nombre))
                .Select(place => place.Nombre.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return names.Length == 0 ? "No hay lugares activos disponibles en el mapa del campus."
                : $"Lugares disponibles (mostrando {Math.Min(10, names.Length)} de {names.Length}): "
                    + string.Join(", ", names.Take(10))
                    + ". Abre los enlaces debajo de esta respuesta para verlos en el mapa del campus.";
        }
        if (!IntentPrefixes.Any(normalizedQuestion.Contains)) return null;
        // A catalog name verifies identity, never opening hours or institutional rules.
        if (normalizedQuestion.Split(' ').Any(word => DocumentaryTerms.Contains(word))) return null;

        var matches = places
            .Where(place => !string.IsNullOrWhiteSpace(place.Nombre))
            .Select(place => (Place: place, Name: Normalize(place.Nombre)))
            .Where(item => item.Name.Length > 1 && ContainsPhrase(normalizedQuestion, item.Name))
            .Select(item => item.Place.Nombre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

        return matches.Length == 0
            ? null
            : $"Encontré {string.Join(", ", matches)}. Abre el enlace debajo de esta respuesta para verla en el mapa del campus.";
    }

    private static bool ContainsPhrase(string text, string phrase) =>
        $" {text} ".Contains($" {phrase} ", StringComparison.Ordinal);

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }

        return string.Join(" ", builder.ToString()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
