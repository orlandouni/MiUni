namespace MiUni.Api.Identity;

public static class AppRoles
{
    public const string Usuario = "Estudiante";
    public const string Propietario = "Propietario";
    public const string Administrador = "Administrador";
    public const string GestionNegocios = Propietario + "," + Administrador;
}
