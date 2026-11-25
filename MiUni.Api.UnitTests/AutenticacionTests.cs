using Microsoft.AspNetCore.Mvc;
using MiUni.Api.Controllers;
using MiUni.Api.Identity;

namespace MiUni.Api.UnitTests;

// Módulo 1: Autenticación y gestión de usuarios (registro, login y perfil).
[TestClass]
public class AutenticacionTests
{
    private static RegisterRequest RegistroValido() => new()
    {
        Nombre = "Ana López",
        Email = "a123456@unison.mx",
        Password = "Segura2026",
        Semestre = 5
    };

    // El controlador revisa las reglas del registro antes de tocar la base de datos,
    // así que estas pruebas no necesitan UserManager ni correo.
    private static AuthController Controlador() => new(null!, null!, null!, null!, null!, null!);

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("Un registro con nombre, correo institucional y contraseña de 8+ caracteres es válido.")]
    public void Registro_DatosCompletos_PasaLaValidacion()
    {
        var errores = Apoyo.Validar(RegistroValido());

        Assert.AreEqual(0, errores.Count, "Resultado esperado: ningún error de validación.");
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Un correo sin formato válido se rechaza con un mensaje en español.")]
    public void Registro_CorreoMalFormado_DaError()
    {
        var request = RegistroValido();
        request.Email = "correo-sin-arroba";

        var errores = Apoyo.Validar(request);

        Assert.AreEqual(1, errores.Count);
        Assert.AreEqual("El correo no tiene un formato válido.", errores[0].ErrorMessage);
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Iniciar sesión sin contraseña no es válido.")]
    public void Login_SinContrasena_DaError()
    {
        var errores = Apoyo.Validar(new LoginRequest { Email = "a123456@unison.mx", Password = null! });

        Assert.AreEqual(1, errores.Count);
        Assert.AreEqual("La contraseña es obligatoria.", errores[0].ErrorMessage);
    }

    [DataTestMethod, TestCategory(Caso.Limite)]
    [Description("La contraseña debe tener al menos 8 caracteres: 7 falla, 8 pasa.")]
    [DataRow("1234567", false, DisplayName = "7 caracteres: rechazada")]
    [DataRow("12345678", true, DisplayName = "8 caracteres: aceptada")]
    public void Registro_LongitudMinimaDeContrasena(string password, bool esperadoValido)
    {
        var request = RegistroValido();
        request.Password = password;

        var esValido = Apoyo.Validar(request).Count == 0;

        Assert.AreEqual(esperadoValido, esValido);
    }

    [DataTestMethod, TestCategory(Caso.Limite)]
    [Description("El semestre del perfil solo admite de 1 a 12.")]
    [DataRow((short)0, false, DisplayName = "Semestre 0: rechazado")]
    [DataRow((short)1, true, DisplayName = "Semestre 1: aceptado")]
    [DataRow((short)12, true, DisplayName = "Semestre 12: aceptado")]
    [DataRow((short)13, false, DisplayName = "Semestre 13: rechazado")]
    public void Perfil_RangoDeSemestre(short semestre, bool esperadoValido)
    {
        var request = new UpdateProfileRequest { Nombre = "Ana López", Semestre = semestre };

        var esValido = Apoyo.Validar(request).Count == 0;

        Assert.AreEqual(esperadoValido, esValido);
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("Un estudiante solo puede registrarse con su correo institucional @unison.mx.")]
    public async Task Registro_EstudianteConCorreoPersonal_EsRechazado()
    {
        var request = RegistroValido();
        request.Email = "ana@gmail.com";

        var resultado = await Controlador().Register(request);

        var badRequest = resultado as BadRequestObjectResult;
        Assert.IsNotNull(badRequest, "Resultado esperado: 400 Bad Request.");
        StringAssert.Contains(Apoyo.Json(badRequest.Value), "correo institucional (@unison.mx)");
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("Nadie puede registrarse como Administrador: solo Estudiante o Propietario.")]
    public async Task Registro_ComoAdministrador_EsRechazado()
    {
        var request = RegistroValido();
        request.TipoCuenta = AppRoles.Administrador;

        var resultado = await Controlador().Register(request);

        var badRequest = resultado as BadRequestObjectResult;
        Assert.IsNotNull(badRequest, "Resultado esperado: 400 Bad Request.");
        StringAssert.Contains(Apoyo.Json(badRequest.Value), "estudiante o de propietario");
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("Los roles que gestionan negocios son Propietario y Administrador.")]
    public void Roles_GestionDeNegocios_IncluyePropietarioYAdministrador()
    {
        var roles = AppRoles.GestionNegocios.Split(',');

        CollectionAssert.AreEquivalent(new[] { "Propietario", "Administrador" }, roles);
        CollectionAssert.DoesNotContain(roles, AppRoles.Usuario);
    }
}
