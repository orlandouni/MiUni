using System;
using System.Collections.Generic;
using MiUni.Api.Enums;
namespace MiUni.Api.Models;

public partial class Reporteusuario
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid? LugarId { get; set; }

    public Guid? ResenaId { get; set; }

    public string Motivo { get; set; } = null!;

    public virtual Lugar? Lugar { get; set; }

    public virtual Resena? Resena { get; set; }
    public EstadoReporte Estado { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
