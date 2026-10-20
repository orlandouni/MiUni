using System.Collections.Generic;

namespace MiUni.Api.Models;

public partial class Documentofuente
{
    public virtual ICollection<Ragchunk> Ragchunks { get; set; } = new List<Ragchunk>();
}
