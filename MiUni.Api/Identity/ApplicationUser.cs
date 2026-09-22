using Microsoft.AspNetCore.Identity;
using MiUni.Api.Enums;
using MiUni.Api.Models;

namespace MiUni.Api.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Nombre { get; set; } = null!;

    public Guid? CarreraId { get; set; }

    public short? Semestre { get; set; }

    public DateTime FechaRegistro { get; set; }

    public RolUsuario Rol { get; set; }

    public virtual Carrera? Carrera { get; set; }

    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

    public virtual ICollection<Historialchat> Historialchats { get; set; } = new List<Historialchat>();

    public virtual ICollection<Reporteusuario> Reporteusuarios { get; set; } = new List<Reporteusuario>();

    public virtual ICollection<Resena> Resenas { get; set; } = new List<Resena>();
}
