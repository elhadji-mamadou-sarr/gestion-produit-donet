// Controllers/AuthController.cs
using GestionProduits.Api.Data;
using GestionProduits.Api.Dtos;  
using GestionProduits.Api.Models;
using GestionProduits.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;                          // ← ajoute

namespace GestionProduits.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ITokenService _tokenSvc;
        private readonly ILogger<AuthController> _log;

        // ← ajoute ces 2 compteurs
        private static readonly Counter _loginCounter = Metrics
            .CreateCounter("auth_login_total", "Nombre de connexions", "status");

        private static readonly Counter _registerCounter = Metrics
            .CreateCounter("auth_register_total", "Nombre d'inscriptions");

        public AuthController(
            AppDbContext db,
            ITokenService tokenSvc,
            ILogger<AuthController> log)
        {
            _db       = db;
            _tokenSvc = tokenSvc;
            _log      = log;
        }

        // ── POST api/auth/register ────────────────────────────────────────
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLowerInvariant()))
                return Conflict(new { message = "Email déjà utilisé" });

            var user = new User
            {
                FirstName    = dto.FirstName,
                LastName     = dto.LastName,
                Email        = dto.Email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _registerCounter.Inc();                                    // ← ajoute
            _log.LogInformation("Nouvel utilisateur inscrit : {Email}", user.Email);
            return Ok(await BuildAuthResponse(user));
        }

        // ── POST api/auth/login ───────────────────────────────────────────
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLowerInvariant());

            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _loginCounter.WithLabels("failed").Inc();              // ← ajoute
                _log.LogWarning("Tentative échouée : {Email}", dto.Email);
                return Unauthorized(new { message = "Email ou mot de passe incorrect" });
            }

            if (!user.IsActive)
                return Forbid();

            _loginCounter.WithLabels("success").Inc();                 // ← ajoute
            _log.LogInformation("Connexion réussie : {Email}", user.Email);
            return Ok(await BuildAuthResponse(user));
        }

        // ── POST api/auth/refresh ─────────────────────────────────────────
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.RefreshToken == dto.RefreshToken &&
                u.RefreshTokenExpiry > DateTime.UtcNow);

            if (user is null)
                return Unauthorized(new { message = "Refresh token invalide ou expiré" });

            return Ok(await BuildAuthResponse(user));
        }

        // ── POST api/auth/logout ──────────────────────────────────────────
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenDto dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == dto.RefreshToken);

            if (user is not null)
            {
                user.RefreshToken       = null;
                user.RefreshTokenExpiry = null;
                await _db.SaveChangesAsync();
            }

            return NoContent();
        }

        // ── Méthode privée ────────────────────────────────────────────────
        private async Task<AuthResponseDto> BuildAuthResponse(User user)
        {
            var refresh             = _tokenSvc.GenerateRefreshToken();
            user.RefreshToken       = refresh;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();

            return new AuthResponseDto(
                AccessToken:  _tokenSvc.GenerateAccessToken(user),
                RefreshToken: refresh,
                User: new UserDto(
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role,
                    user.IsActive,
                    user.AvatarUrl,
                    user.Phone,
                    user.Department,
                    user.CreatedAt
                )
            );
        }
    }
}