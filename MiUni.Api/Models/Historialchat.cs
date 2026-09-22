using System;
using System.Collections.Generic;
using MiUni.Api.Identity;

namespace MiUni.Api.Models;

public partial class Historialchat
{
    public Guid Id { get; set; }

    public Guid SesionId { get; set; }

    public Guid UsuarioId { get; set; }

    public string Rol { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public string? ContextoUtilizado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ApplicationUser Usuario { get; set; } = null!;
}
