using System;
using System.Collections.Generic;
using MiUni.Api.Identity;

namespace MiUni.Api.Models;

public partial class Favorito
{
    public Guid UsuarioId { get; set; }

    public Guid LugarId { get; set; }

    public DateTime FechaAgregado { get; set; }

    public virtual Lugar Lugar { get; set; } = null!;

    public virtual ApplicationUser Usuario { get; set; } = null!;
}
