using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace MiUni.Api.Models;

public partial class Camino
{
    public Guid Id { get; set; }

    public LineString Geometria { get; set; } = null!;

    public bool Transitable { get; set; }

    public string? TipoSuperficie { get; set; }
}
