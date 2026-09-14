using System;
using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Resena
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid LugarId { get; set; }

    public short Calificacion { get; set; }

    public string? Comentario { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Lugar Lugar { get; set; } = null!;

    public virtual ICollection<Reporteusuario> Reporteusuarios { get; set; } = new List<Reporteusuario>();

    public virtual Usuario Usuario { get; set; } = null!;
}
