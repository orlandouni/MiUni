namespace MiUni.Api.DTOs;

public class LugarDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public short? Piso { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public CategoriaDto Categoria { get; set; } = null!;
}

public class CategoriaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Icono { get; set; }
}