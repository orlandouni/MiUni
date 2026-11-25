using MiUni.Api.DTOs;
using MiUni.Api.Models;
using MiUni.Api.Services;

namespace MiUni.Api.UnitTests;

// Módulo 3: Mapa del campus (rutas, horario de los lugares y búsqueda de lugares desde el chat).
[TestClass]
public class MapaTests
{
    // Miércoles 25 de noviembre de 2026 (DiaSemana 3 en MiUni: 1 = lunes … 7 = domingo).
    private static DateTime Miercoles(int hora, int minuto = 0) => new(2026, 11, 25, hora, minuto, 0);

    private static Horariooperacion Horario(short dia, string abre, string cierra) => new()
    {
        Id = Guid.NewGuid(), DiaSemana = dia, HoraApertura = TimeOnly.Parse(abre), HoraCierre = TimeOnly.Parse(cierra)
    };

    private static RutaRequest Ruta(double latA, double lonA, double radio = 100) => new()
    {
        A = new PuntoRutaDto { Latitud = latA, Longitud = lonA },
        B = new PuntoRutaDto { Latitud = 29.0825, Longitud = -110.9614 },
        RadioConexionMetros = radio
    };

    private static readonly ChatPlace[] Lugares =
    [
        new("Biblioteca Central", "Biblioteca"),
        new("Cafetería de Ingeniería", "Comida"),
        new("Edificio 5K", "Aulas")
    ];

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("Una cafetería que abre de 07:00 a 15:00 está abierta el mismo día a las 10:30.")]
    public void Lugar_DentroDeSuHorario_EstaAbierto()
    {
        var horarios = new[] { Horario(3, "07:00", "15:00") };

        Assert.IsTrue(HorarioLugar.EstaAbierto(horarios, Miercoles(10, 30)));
    }

    [DataTestMethod, TestCategory(Caso.Limite)]
    [Description("A la hora exacta de apertura ya está abierto; a la hora exacta de cierre ya está cerrado.")]
    [DataRow(6, 59, false, DisplayName = "06:59: cerrado")]
    [DataRow(7, 0, true, DisplayName = "07:00 (apertura): abierto")]
    [DataRow(14, 59, true, DisplayName = "14:59: abierto")]
    [DataRow(15, 0, false, DisplayName = "15:00 (cierre): cerrado")]
    public void Lugar_HorasFrontera(int hora, int minuto, bool esperadoAbierto)
    {
        var horarios = new[] { Horario(3, "07:00", "15:00") };

        Assert.AreEqual(esperadoAbierto, HorarioLugar.EstaAbierto(horarios, Miercoles(hora, minuto)));
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("Un horario de 22:00 a 02:00 del martes cruza la medianoche: sigue abierto el miércoles a la 01:00.")]
    public void Lugar_HorarioQueCruzaMedianoche_SigueAbiertoAlDiaSiguiente()
    {
        var horarios = new[] { Horario(2, "22:00", "02:00") };

        Assert.IsTrue(HorarioLugar.EstaAbierto(horarios, Miercoles(1)));
        Assert.IsFalse(HorarioLugar.EstaAbierto(horarios, Miercoles(2)));
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Un lugar sin horarios registrados, o que no abre ese día, se considera cerrado.")]
    public void Lugar_SinHorarioParaEseDia_EstaCerrado()
    {
        Assert.IsFalse(HorarioLugar.EstaAbierto([], Miercoles(10)));
        Assert.IsFalse(HorarioLugar.EstaAbierto([Horario(1, "07:00", "15:00")], Miercoles(10)));
    }

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("Una ruta entre dos puntos del campus con coordenadas válidas pasa la validación.")]
    public void Ruta_CoordenadasDelCampus_SonValidas()
    {
        Assert.AreEqual(0, Apoyo.Validar(Ruta(29.0819, -110.9594)).Count);
    }

    [DataTestMethod, TestCategory(Caso.Limite)]
    [Description("Latitud entre -90 y 90, longitud entre -180 y 180, radio de conexión entre 1 y 1000 m.")]
    [DataRow(90.0, -180.0, 100.0, true, DisplayName = "Latitud 90 / longitud -180: válidas")]
    [DataRow(90.1, 0.0, 100.0, false, DisplayName = "Latitud 90.1: inválida")]
    [DataRow(0.0, 180.1, 100.0, false, DisplayName = "Longitud 180.1: inválida")]
    [DataRow(29.08, -110.96, 1.0, true, DisplayName = "Radio 1 m: válido")]
    [DataRow(29.08, -110.96, 1000.0, true, DisplayName = "Radio 1000 m: válido")]
    [DataRow(29.08, -110.96, 0.0, false, DisplayName = "Radio 0 m: inválido")]
    [DataRow(29.08, -110.96, 1000.5, false, DisplayName = "Radio 1000.5 m: inválido")]
    public void Ruta_ValoresFrontera(double lat, double lon, double radio, bool esperadoValido)
    {
        var request = Ruta(lat, lon, radio);
        var errores = Apoyo.Validar(request);
        errores.AddRange(Apoyo.Validar(request.A));

        Assert.AreEqual(esperadoValido, errores.Count == 0);
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Un punto sin latitud no es una ruta válida.")]
    public void Ruta_PuntoSinLatitud_DaError()
    {
        var punto = new PuntoRutaDto { Latitud = null, Longitud = -110.96 };

        var errores = Apoyo.Validar(punto);

        Assert.AreEqual(1, errores.Count);
        CollectionAssert.Contains(errores[0].MemberNames.ToList(), nameof(PuntoRutaDto.Latitud));
    }

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("\"¿Dónde está la biblioteca central?\" encuentra el lugar aunque se escriba sin mayúsculas.")]
    public void Chat_PreguntaDondeEsta_EncuentraElLugar()
    {
        var respuesta = ChatPlaceRouter.TryBuildAnswer("¿Dónde está la biblioteca central?", Lugares);

        Assert.IsNotNull(respuesta);
        StringAssert.StartsWith(respuesta, "Encontré Biblioteca Central.");
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("Preguntar a qué hora abre un lugar no se contesta con el catálogo del mapa (lo responde el RAG).")]
    public void Chat_PreguntaPorHorarioDeUnLugar_NoLaContestaElMapa()
    {
        Assert.IsNull(ChatPlaceRouter.TryBuildAnswer("¿Dónde está la biblioteca central y a qué hora abre?", Lugares));
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Un lugar que no existe en el catálogo no inventa resultados.")]
    public void Chat_LugarInexistente_NoDevuelveRespuesta()
    {
        Assert.IsNull(ChatPlaceRouter.TryBuildAnswer("¿Dónde está el estadio de béisbol?", Lugares));
    }

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("La lista de lugares muestra como máximo 10 aunque haya más.")]
    public void Chat_ListaDeLugares_MuestraMaximoDiez()
    {
        var muchos = Enumerable.Range(1, 15).Select(i => new ChatPlace($"Edificio {i}", null));

        var respuesta = ChatPlaceRouter.TryBuildAnswer("¿Qué lugares hay?", muchos);

        StringAssert.Contains(respuesta, "mostrando 10 de 15");
        Assert.IsFalse(respuesta!.Contains("Edificio 11"));
    }
}
