using MiUni.Api.DTOs;

namespace MiUni.Api.Services;

public interface IRutaService
{
    Task<RutaResponse?> CalcularAsync(RutaRequest request, CancellationToken cancellationToken);
}
