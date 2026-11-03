using System.ComponentModel.DataAnnotations;

namespace MiUni.Api.DTOs;

public class GuardarLugarRequest
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(2000)] public string? Descripcion { get; set; }
    public Guid CategoriaId { get; set; }
    [Required, Range(-90d, 90d)] public double? Latitud { get; set; }
    [Required, Range(-180d, 180d)] public double? Longitud { get; set; }
    public short? Piso { get; set; }
    public bool Activo { get; set; } = true;
}

public class GuardarCategoriaRequest
{
    [Required, StringLength(100)] public string Nombre { get; set; } = null!;
    [StringLength(500)] public string? Icono { get; set; }
}

public record LugarAdminDto(Guid Id, string Nombre, string? Descripcion, Guid CategoriaId,
    string NombreCategoria, double Latitud, double Longitud, short? Piso, bool Activo,
    DateTime FechaCreacion, Guid? PropietarioId, string? EstadoSolicitud);

public record ConteoDto(string Etiqueta, int Cantidad);

public record LugarDestacadoDto(Guid Id, string Nombre, int Resenas, double? Promedio, int Favoritos);

public record PanelAdminDto(int Usuarios, int UsuariosNuevos30Dias, int UsuariosBloqueados,
    int LugaresActivos, int LugaresInactivos, int Categorias, int Resenas, double? PromedioCalificacion,
    int ReportesPendientes, int SolicitudesPendientes, int SesionesChat7Dias, int MensajesChat7Dias,
    IEnumerable<ConteoDto> RegistrosPorDia, IEnumerable<ConteoDto> LugaresPorCategoria,
    IEnumerable<LugarDestacadoDto> LugaresDestacados);

public record UsuarioAdminDto(Guid Id, string Nombre, string? Email, Guid? CarreraId, string? NombreCarrera,
    short? Semestre, DateTime FechaRegistro, IEnumerable<string> Roles, bool Bloqueado,
    int Resenas, int Reportes, int Negocios);

public record PaginaDto<T>(IEnumerable<T> Items, int Total, int Pagina, int TamanoPagina);

public class RolesUsuarioRequest
{
    [Required] public List<string> Roles { get; set; } = [];
}

public record ResenaAdminDto(Guid Id, Guid UsuarioId, string NombreUsuario, string? EmailUsuario,
    Guid LugarId, string NombreLugar, short Calificacion, string? Comentario, DateTime FechaCreacion,
    int ReportesPendientes, int ReportesTotales);

public record NegocioAdminDto(Guid Id, string Nombre, string? Descripcion, string NombreCategoria,
    Guid? PropietarioId, string? NombrePropietario, string? EmailPropietario, string? EstadoSolicitud,
    bool Activo, bool EsAmbulante, double Latitud, double Longitud, DateTime FechaCreacion);

public class GuardarCarreraRequest
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(150)] public string? Facultad { get; set; }
}

public record CarreraAdminDto(Guid Id, string Nombre, string? Facultad, int Usuarios);
