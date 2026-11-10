using MiUni.Api.Models;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class HorarioLugarTests
{
    private static Horariooperacion H(short dia, string abre, string cierra) =>
        new() { DiaSemana = dia, HoraApertura = TimeOnly.Parse(abre), HoraCierre = TimeOnly.Parse(cierra) };

    [Theory]
    // 2026-11-10 es martes (DiaSemana 2).
    [InlineData("2026-11-10 08:00", true)]
    [InlineData("2026-11-10 07:59", false)]
    [InlineData("2026-11-10 14:00", false)]  // cierra a las 14:00
    [InlineData("2026-11-11 09:00", false)]  // miércoles sin horario
    [InlineData("2026-11-10 23:30", true)]   // turno nocturno del martes
    [InlineData("2026-11-11 01:30", true)]   // sigue abierto pasada la medianoche
    [InlineData("2026-11-11 02:00", false)]
    public void CalculaSiEstaAbierto(string momento, bool esperado)
    {
        var horarios = new[] { H(2, "08:00", "14:00"), H(2, "22:00", "02:00") };
        Assert.Equal(esperado, HorarioLugar.EstaAbierto(horarios, DateTime.Parse(momento)));
    }

    [Fact]
    public void ConvierteAHoraDeHermosillo()
    {
        var local = HorarioLugar.AhoraEnCampus(new DateTimeOffset(2026, 11, 10, 15, 0, 0, TimeSpan.Zero));
        Assert.Equal(new DateTime(2026, 11, 10, 8, 0, 0), local);
    }
}
