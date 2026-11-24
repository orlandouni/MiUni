using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Documentofuente
{
    public string TemporalStatus { get; set; } = "desconocido";
    public int? DocumentYear { get; set; }
    public string? AcademicPeriod { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public Guid? SupersededBy { get; set; }

    public virtual ICollection<Ragchunk> Ragchunks { get; set; } = new List<Ragchunk>();
}
