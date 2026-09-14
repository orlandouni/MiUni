using System;
using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Puntointeresinterno
{
    public Guid Id { get; set; }

    public Guid LugarId { get; set; }

    public string Tipo { get; set; } = null!;

    public short? Piso { get; set; }

    public string? Descripcion { get; set; }

    public virtual Lugar Lugar { get; set; } = null!;
}
