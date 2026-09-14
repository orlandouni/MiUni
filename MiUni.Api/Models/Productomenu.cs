using System;
using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Productomenu
{
    public Guid Id { get; set; }

    public Guid LugarId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    public bool EstaDisponible { get; set; }

    public virtual Lugar Lugar { get; set; } = null!;
}
