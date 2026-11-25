using Microsoft.AspNetCore.Mvc;
using MiUni.Api.Controllers;
using MiUni.Api.Identity;

namespace MiUni.Api.UnitTests;

// Módulo 1: Autenticación y gestión de usuarios (registro e inicio de sesión).
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

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("La contraseña debe tener al menos 8 caracteres: con 7 se rechaza y con 8 se acepta.")]
    public void Registro_ContrasenaEnElMinimo_SieteFallaYOchoPasa()
    {
        var conSiete = RegistroValido();
        conSiete.Password = "1234567";
        var conOcho = RegistroValido();
        conOcho.Password = "12345678";

        var erroresSiete = Apoyo.Validar(conSiete);
        var erroresOcho = Apoyo.Validar(conOcho);

        Assert.AreEqual("La contraseña debe tener al menos 8 caracteres.", erroresSiete.Single().ErrorMessage);
        Assert.AreEqual(0, erroresOcho.Count);
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
}
