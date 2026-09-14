using System;
using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Etiquetum
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Lugar> Lugars { get; set; } = new List<Lugar>();
}
