using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiUni.Api.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Enums;
using MiUni.Api.Models;
using MiUni.Api.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
namespace MiUni.Api.Controllers;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly MiUniDbContext _context;
    private readonly ICorreoService _correo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        MiUniDbContext context,
        ICorreoService correo,
        IMemoryCache cache,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
        _correo = correo;
        _cache = cache;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
    // Sólo se puede elegir entre estudiante y propietario; el rol de administrador
    // nunca se asigna desde el registro.
    var tipoCuenta = string.IsNullOrWhiteSpace(request.TipoCuenta) ? AppRoles.Usuario : request.TipoCuenta.Trim();
    if (tipoCuenta != AppRoles.Usuario && tipoCuenta != AppRoles.Propietario)
    {
        return BadRequest(new { message = "Elige una cuenta de estudiante o de propietario de negocio." });
    }
    // Los propietarios pueden no pertenecer a la universidad, así que sólo a los
    // estudiantes se les exige el correo institucional.
    if (tipoCuenta == AppRoles.Usuario && !request.Email.EndsWith("@unison.mx", StringComparison.OrdinalIgnoreCase))
    {
        return BadRequest(new { message = "Debes registrarte con tu correo institucional (@unison.mx)." });
    }
    var user = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email,
        Nombre = request.Nombre,
        CarreraId = request.CarreraId,
        Semestre = request.Semestre,
        FechaRegistro = DateTime.UtcNow,
        Rol = RolUsuario.Estudiante
    };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var roles = tipoCuenta == AppRoles.Propietario
            ? new[] { AppRoles.Usuario, AppRoles.Propietario }
            : new[] { AppRoles.Usuario };
        var roleResult = await _userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Problem("No fue posible asignar los permisos de la cuenta.");
        }

        var correoEnviado = await EnviarVerificacion(user, tipoCuenta == AppRoles.Propietario);

        return Ok(new
        {
            message = correoEnviado
                ? "Usuario registrado correctamente. Revisa tu correo para verificar tu cuenta."
                : "Usuario registrado, pero no pudimos enviar el correo de verificación. Solicita uno nuevo.",
            requiereVerificacion = true,
            correoEnviado
        });
    }

    // Confirma el correo con el enlace recibido. Es idempotente para cuentas ya verificadas.
    [HttpPost("verificar-correo")]
    public async Task<IActionResult> VerificarCorreo(VerificarCorreoRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UsuarioId.ToString());
        if (user == null) return BadRequest(new { message = "El enlace de verificación no es válido." });
        if (user.EmailConfirmed) return Ok(new { message = "Tu correo ya estaba verificado.", yaVerificado = true });

        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token)); }
        catch (FormatException) { return BadRequest(new { message = "El enlace de verificación no es válido." }); }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded) return Ok(new { message = "¡Listo! Tu correo quedó verificado.", yaVerificado = false });

        // Dos aperturas simultáneas del mismo enlace: una confirma y la otra choca por concurrencia.
        var actual = await _userManager.FindByIdAsync(request.UsuarioId.ToString());
        return actual?.EmailConfirmed == true
            ? Ok(new { message = "Tu correo ya estaba verificado.", yaVerificado = true })
            : BadRequest(new { message = "El enlace venció o ya no es válido. Solicita uno nuevo." });
    }

    // Siempre responde igual para no revelar qué correos están registrados.
    [HttpPost("reenviar-verificacion")]
    public async Task<IActionResult> ReenviarVerificacion(ReenviarVerificacionRequest request)
    {
        var respuesta = Ok(new { message = "Si la cuenta existe y aún no está verificada, te enviamos un nuevo correo." });
        var clave = "reenvio:" + request.Email.Trim().ToLowerInvariant();
        if (_cache.TryGetValue(clave, out _)) return respuesta;
        _cache.Set(clave, true, TimeSpan.FromSeconds(60));

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user != null && !user.EmailConfirmed)
            await EnviarVerificacion(user, await _userManager.IsInRoleAsync(user, AppRoles.Propietario));
        return respuesta;
    }

    private async Task<bool> EnviarVerificacion(ApplicationUser user, bool esPropietario)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var frontend = (_configuration["App:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var enlace = $"{frontend}/verificar-correo?usuario={user.Id}&token={WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))}";
        var nombre = WebUtility.HtmlEncode(user.Nombre);
        var motivo = esPropietario
            ? "Confirma que este correo es válido para administrar tu negocio en MiUni."
            : "Confirma tu correo institucional para empezar a usar MiUni.";
        var html = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:auto;color:#182f48">
              <h1 style="color:#102e50">Hola, {nombre}</h1>
              <p>{motivo}</p>
              <p style="margin:28px 0"><a href="{enlace}" style="background:#245bdf;color:#fff;padding:12px 22px;border-radius:10px;text-decoration:none;font-weight:600">Verificar mi correo</a></p>
              <p style="color:#5e6f80;font-size:13px">El enlace vence en 24 horas. Si no creaste esta cuenta, ignora este mensaje.</p>
              <p style="color:#5e6f80;font-size:12px;word-break:break-all">{enlace}</p>
            </div>
            """;
        var texto = $"Hola, {user.Nombre}.\n\n{motivo}\n\nAbre este enlace (vence en 24 horas):\n{enlace}";
        try
        {
            await _correo.EnviarAsync(user.Email!, "Verifica tu correo en MiUni", html, texto);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo de verificación a la cuenta {UserId}.", user.Id);
            return false;
        }
    }

[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);

    if (user == null)
    {
        return Unauthorized(new
        {
            message = "Correo o contraseña incorrectos."
        });
    }

    var result = await _userManager.CheckPasswordAsync(user, request.Password);

    if (result && await _userManager.IsLockedOutAsync(user))
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Tu cuenta está suspendida. Contacta a la administración de MiUni."
        });
    }

    if (!result)
    {
        return Unauthorized(new
        {
            message = "Correo o contraseña incorrectos."
        });
    }

    if (!user.EmailConfirmed)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Verifica tu correo antes de iniciar sesión. Revisa tu bandeja de entrada o solicita un nuevo enlace.",
            codigo = "CORREO_NO_VERIFICADO"
        });
    }

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email!)
    };
    var roles = await _userManager.GetRolesAsync(user);
    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
    );

    var credentials = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256
    );

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: credentials
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Ok(new
    {
        token = tokenString,
        expires = token.ValidTo,
        roles
    });
}

[Authorize]
[HttpGet("me")]
public async Task<IActionResult> Me()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var user = await _userManager.FindByIdAsync(userId!);

    if (user == null)
    {
        return Unauthorized();
    }

    var roles = await _userManager.GetRolesAsync(user);

    return Ok(new
    {
        id = user.Id,
        nombre = user.Nombre,
        email = user.Email,
        carreraId = user.CarreraId,
        semestre = user.Semestre,
        fechaRegistro = user.FechaRegistro,
        rol = roles.Contains(AppRoles.Administrador) ? AppRoles.Administrador
            : roles.Contains(AppRoles.Propietario) ? AppRoles.Propietario : AppRoles.Usuario,
        roles
    });
}

[Authorize]
[HttpPut("profile")]
public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var user = await _userManager.FindByIdAsync(userId!);
    if (user == null) return Unauthorized();
    if (request.CarreraId.HasValue && !await _context.Carreras.AnyAsync(c => c.Id == request.CarreraId.Value, ct))
        return BadRequest(new { message = "La carrera seleccionada no existe." });
    user.Nombre = request.Nombre.Trim();
    user.CarreraId = request.CarreraId;
    user.Semestre = request.Semestre;
    var result = await _userManager.UpdateAsync(user);
    return result.Succeeded ? NoContent() : BadRequest(result.Errors);
}
}


public class RegisterRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = null!;

    public Guid? CarreraId { get; set; }

    public short? Semestre { get; set; }

    // "Estudiante" (por defecto) o "Propietario".
    [StringLength(20)]
    public string? TipoCuenta { get; set; }
}

public class VerificarCorreoRequest
{
    [Required] public Guid UsuarioId { get; set; }
    [Required] public string Token { get; set; } = null!;
}

public class ReenviarVerificacionRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = null!;
}

public class LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = null!;
}

public class UpdateProfileRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Nombre { get; set; } = null!;

    public Guid? CarreraId { get; set; }

    [Range(1, 12)]
    public short? Semestre { get; set; }
}
