using System;
using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Horariooperacion
{
    public Guid Id { get; set; }

    public Guid LugarId { get; set; }

    public short DiaSemana { get; set; }

    public TimeOnly HoraApertura { get; set; }

    public TimeOnly HoraCierre { get; set; }

    public virtual Lugar Lugar { get; set; } = null!;
}
