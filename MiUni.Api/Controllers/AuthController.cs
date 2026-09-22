using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiUni.Api.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using MiUni.Api.Enums;
namespace MiUni.Api.Controllers;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
         if (!request.Email.EndsWith("@unison.mx", StringComparison.OrdinalIgnoreCase))
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

        return Ok(new
        {
            message = "Usuario registrado correctamente."
        });
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

    if (!result)
    {
        return Unauthorized(new
        {
            message = "Correo o contraseña incorrectos."
        });
    }

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email!)
    };

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
        expires = token.ValidTo
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

    return Ok(new
    {
        id = user.Id,
        nombre = user.Nombre,
        email = user.Email,
        carreraId = user.CarreraId,
        semestre = user.Semestre,
        fechaRegistro = user.FechaRegistro,
        rol = user.Rol.ToString()
    });
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
}

public class LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = null!;
}