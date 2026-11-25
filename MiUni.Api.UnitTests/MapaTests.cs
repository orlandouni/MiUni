using MiUni.Api.DTOs;
using MiUni.Api.Models;
using MiUni.Api.Services;

namespace MiUni.Api.UnitTests;

// Módulo 3: Mapa del campus (horario de los lugares y rutas a pie).
[TestClass]
public class MapaTests
{
    // Miércoles 25 de noviembre de 2026 (DiaSemana 3 en MiUni: 1 = lunes … 7 = domingo).
    private static DateTime Miercoles(int hora, int minuto = 0) => new(2026, 11, 25, hora, minuto, 0);

    private static Horariooperacion Horario(short dia, string abre, string cierra) => new()
    {
        Id = Guid.NewGuid(), DiaSemana = dia, HoraApertura = TimeOnly.Parse(abre), HoraCierre = TimeOnly.Parse(cierra)
    };

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("Una cafetería que abre de 07:00 a 15:00 está abierta el mismo día a las 10:30.")]
    public void Lugar_DentroDeSuHorario_EstaAbierto()
    {
        var horarios = new[] { Horario(3, "07:00", "15:00") };

        Assert.IsTrue(HorarioLugar.EstaAbierto(horarios, Miercoles(10, 30)));
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Un punto de la ruta sin latitud no es válido.")]
    public void Ruta_PuntoSinLatitud_DaError()
    {
        var punto = new PuntoRutaDto { Latitud = null, Longitud = -110.96 };

        var errores = Apoyo.Validar(punto);

        Assert.AreEqual(1, errores.Count);
        CollectionAssert.Contains(errores[0].MemberNames.ToList(), nameof(PuntoRutaDto.Latitud));
    }

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("A la hora exacta de apertura ya está abierto; a la hora exacta de cierre ya está cerrado.")]
    public void Lugar_HoraExactaDeAperturaYCierre()
    {
        var horarios = new[] { Horario(3, "07:00", "15:00") };

        Assert.IsTrue(HorarioLugar.EstaAbierto(horarios, Miercoles(7)), "07:00: abierto");
        Assert.IsFalse(HorarioLugar.EstaAbierto(horarios, Miercoles(15)), "15:00: cerrado");
    }

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("El radio de conexión de una ruta debe estar entre 1 y 1000 metros.")]
    public void Ruta_RadioDeConexion_SoloEntre1Y1000Metros()
    {
        RutaRequest Ruta(double radio) => new()
        {
            A = new PuntoRutaDto { Latitud = 29.0819, Longitud = -110.9594 },
            B = new PuntoRutaDto { Latitud = 29.0825, Longitud = -110.9614 },
            RadioConexionMetros = radio
        };

        Assert.AreEqual(0, Apoyo.Validar(Ruta(1)).Count, "1 m: válido");
        Assert.AreEqual(0, Apoyo.Validar(Ruta(1000)).Count, "1000 m: válido");
        Assert.AreEqual(1, Apoyo.Validar(Ruta(0)).Count, "0 m: inválido");
        Assert.AreEqual(1, Apoyo.Validar(Ruta(1000.5)).Count, "1000.5 m: inválido");
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("Un horario de 22:00 a 02:00 del martes cruza la medianoche: sigue abierto el miércoles a la 01:00.")]
    public void Lugar_HorarioQueCruzaMedianoche_SigueAbiertoAlDiaSiguiente()
    {
        var horarios = new[] { Horario(2, "22:00", "02:00") };

        Assert.IsTrue(HorarioLugar.EstaAbierto(horarios, Miercoles(1)), "01:00: abierto");
        Assert.IsFalse(HorarioLugar.EstaAbierto(horarios, Miercoles(2)), "02:00: cerrado");
    }
}
