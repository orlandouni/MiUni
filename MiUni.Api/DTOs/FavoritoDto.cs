namespace MiUni.Api.DTOs;

public record FavoritoDto(Guid LugarId, DateTime FechaAgregado);

public class CrearFavoritoRequest
{
    public Guid LugarId { get; set; }
}
