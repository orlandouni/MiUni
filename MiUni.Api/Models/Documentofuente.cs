using System;
using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Documentofuente
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = null!;

    public string TipoOrigen { get; set; } = null!;

    public string Referencia { get; set; } = null!;

    public DateTime UltimaActualizacion { get; set; }
}
