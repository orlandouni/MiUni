using System;
using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Eventotemporal
{
    public Guid Id { get; set; }

    public Guid? LugarId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public virtual Lugar? Lugar { get; set; }
}
