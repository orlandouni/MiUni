using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace MiUni.Api.Models;

public partial class Lugar
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public Guid CategoriaId { get; set; }

    public string? Descripcion { get; set; }

    public Point Ubicacion { get; set; } = null!;

    public short? Piso { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Categorium Categoria { get; set; } = null!;

    public virtual ICollection<Eventotemporal> Eventotemporals { get; set; } = new List<Eventotemporal>();

    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

    public virtual Foto? Foto { get; set; }

    public virtual ICollection<Horariooperacion> Horariooperacions { get; set; } = new List<Horariooperacion>();

    public virtual ICollection<Productomenu> Productomenus { get; set; } = new List<Productomenu>();

    public virtual ICollection<Puntointeresinterno> Puntointeresinternos { get; set; } = new List<Puntointeresinterno>();

    public virtual ICollection<Reporteusuario> Reporteusuarios { get; set; } = new List<Reporteusuario>();

    public virtual ICollection<Resena> Resenas { get; set; } = new List<Resena>();

    public virtual ICollection<Etiquetum> Tags { get; set; } = new List<Etiquetum>();
}
