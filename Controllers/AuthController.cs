using BCrypt.Net;
using GestionProduits.Api.DTOs;
using GestionProduits.Api.Infrastructure;
using GestionProduits.Api.Models;
using GestionProduits.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionProduits.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, ITokenService tokenSvc, ILogger<AuthController> log)
    : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email.ToLowerInvariant()))
            return Conflict(new { message = "Email déjà utilisé" });

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        log.LogInformation("Nouvel utilisateur inscrit : {Email}", user.Email);
        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLowerInvariant());
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            log.LogWarning("Tentative échouée : {Email}", dto.Email);
            return Unauthorized(new { message = "Email ou mot de passe incorrect" });
        }
        if (!user.IsActive) return Forbid();
        log.LogInformation("Connexion réussie : {Email}", user.Email);
        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == dto.RefreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);
        if (user is null)
            return Unauthorized(new { message = "Refresh token invalide ou expiré" });
        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshToken == dto.RefreshToken);
        if (user is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await db.SaveChangesAsync();
        }
        return NoContent();
    }

    private AuthResponseDto BuildAuthResponse(User user)
    {
        var refresh = tokenSvc.GenerateRefreshToken();
        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        db.SaveChanges();
        return new AuthResponseDto(
            AccessToken: tokenSvc.GenerateAccessToken(user),
            RefreshToken: refresh,
            User: new UserDto(user.Id, user.FirstName, user.LastName, user.Email,
                user.Role, user.IsActive, user.AvatarUrl, user.Phone, user.Department, user.CreatedAt));
    }
}