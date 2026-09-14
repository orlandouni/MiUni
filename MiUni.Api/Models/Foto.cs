using System;
using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Foto
{
    public Guid Id { get; set; }

    public Guid LugarId { get; set; }

    public string StorageKey { get; set; } = null!;

    public bool EsPrincipal { get; set; }

    public DateTime FechaSubida { get; set; }

    public virtual Lugar Lugar { get; set; } = null!;
}
