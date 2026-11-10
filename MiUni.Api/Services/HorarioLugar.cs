using MiUni.Api.Models;

namespace MiUni.Api.Services;

/// <summary>Decide si un lugar está abierto con la hora local del campus (Hermosillo, sin horario de verano).</summary>
public static class HorarioLugar
{
    private static readonly TimeZoneInfo ZonaCampus = Buscar();

    private static TimeZoneInfo Buscar()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Hermosillo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("Hermosillo", TimeSpan.FromHours(-7), "Hermosillo", "Hermosillo"); }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.CreateCustomTimeZone("Hermosillo", TimeSpan.FromHours(-7), "Hermosillo", "Hermosillo"); }
    }

    public static DateTime AhoraEnCampus(DateTimeOffset? utc = null) =>
        TimeZoneInfo.ConvertTime(utc ?? DateTimeOffset.UtcNow, ZonaCampus).DateTime;

    // DiaSemana: 1 = lunes … 7 = domingo. Un horario que cierra antes de abrir (22:00–02:00) cruza la medianoche.
    public static bool EstaAbierto(IEnumerable<Horariooperacion> horarios, DateTime local)
    {
        var hoy = (short)(((int)local.DayOfWeek + 6) % 7 + 1);
        var ayer = (short)(hoy == 1 ? 7 : hoy - 1);
        var hora = TimeOnly.FromDateTime(local);
        return horarios.Any(h =>
            h.HoraApertura <= h.HoraCierre
                ? h.DiaSemana == hoy && h.HoraApertura <= hora && hora < h.HoraCierre
                : (h.DiaSemana == hoy && hora >= h.HoraApertura) || (h.DiaSemana == ayer && hora < h.HoraCierre));
    }
}
