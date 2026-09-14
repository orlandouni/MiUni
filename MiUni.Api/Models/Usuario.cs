using System;
using System.Collections.Generic;
using MiUni.Api.Enums;
namespace MiUni.Api.Models;

public partial class Usuario
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string CorreoInstitucional { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public Guid? CarreraId { get; set; }

    public short? Semestre { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Carrera? Carrera { get; set; }

    public RolUsuario Rol { get; set; }

    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

    public virtual ICollection<Historialchat> Historialchats { get; set; } = new List<Historialchat>();

    public virtual ICollection<Reporteusuario> Reporteusuarios { get; set; } = new List<Reporteusuario>();

    public virtual ICollection<Resena> Resenas { get; set; } = new List<Resena>();
}
