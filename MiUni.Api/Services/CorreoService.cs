using System.Net;
using System.Net.Mail;

namespace MiUni.Api.Services;

public interface ICorreoService
{
    Task EnviarAsync(string destino, string asunto, string html, string texto, CancellationToken ct = default);
}

public class CorreoOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Usuario { get; set; }
    public string? Contrasena { get; set; }
    public string? Remitente { get; set; }
    public string NombreRemitente { get; set; } = "MiUni";
    public bool UsarSsl { get; set; } = true;
}

// Envía por SMTP (Gmail, Outlook, etc.). Sin servidor configurado, en desarrollo
// escribe el correo en la consola para poder probar el flujo sin una cuenta real.
public class SmtpCorreoService(IConfiguration configuration, IHostEnvironment environment,
    ILogger<SmtpCorreoService> logger) : ICorreoService
{
    public async Task EnviarAsync(string destino, string asunto, string html, string texto, CancellationToken ct = default)
    {
        var opciones = configuration.GetSection("Correo").Get<CorreoOptions>() ?? new CorreoOptions();
        if (string.IsNullOrWhiteSpace(opciones.Host))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Configura la sección Correo (SMTP) para enviar correos.");
            logger.LogWarning("SMTP sin configurar. Correo para {Destino} — {Asunto}:\n{Texto}", destino, asunto, texto);
            return;
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(opciones.Remitente ?? opciones.Usuario!, opciones.NombreRemitente),
            Subject = asunto,
            Body = texto,
        };
        mensaje.To.Add(destino);
        mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, "text/html"));

        using var cliente = new SmtpClient(opciones.Host, opciones.Port) { EnableSsl = opciones.UsarSsl };
        if (!string.IsNullOrWhiteSpace(opciones.Usuario))
            cliente.Credentials = new NetworkCredential(opciones.Usuario, opciones.Contrasena);
        await cliente.SendMailAsync(mensaje, ct);
    }
}
