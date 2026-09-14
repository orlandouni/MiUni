using System;
using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Carrera
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Facultad { get; set; }

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
