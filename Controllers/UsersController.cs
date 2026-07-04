// Controllers/UsersController.cs
using System.Security.Claims;
using GestionProduits.Api.Data;
using GestionProduits.Api.Dtos;
using GestionProduits.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;

namespace GestionProduits.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UsersController> _log;


        private static readonly Counter _userCreatedCounter = Metrics
            .CreateCounter("users_created_total", "Nombre total d'utilisateurs créés");

    
        public UsersController(AppDbContext db, ILogger<UsersController> log)
        {
            _db  = db;
            _log = log;
        }

        // ── Profil personnel ──────────────────────────────────────────────

        // GET api/users/me
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetMe()
        {
            var id   = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _db.Users.FindAsync(id);
            return user is null ? NotFound() : Ok(ToDto(user));
        }

        // PUT api/users/me
        [HttpPut("me")]
        public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileDto dto)
        {
            var id   = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();

            user.FirstName  = dto.FirstName;
            user.LastName   = dto.LastName;
            user.Phone      = dto.Phone;
            user.Department = dto.Department;
            user.UpdatedAt  = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(ToDto(user));
        }

        // PUT api/users/me/password
        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var id   = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Mot de passe actuel incorrect" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt    = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ── Admin CRUD ────────────────────────────────────────────────────

        // GET api/users
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
            [FromQuery] int page      = 1,
            [FromQuery] int pageSize  = 10,
            [FromQuery] string search = "",
            [FromQuery] string role   = "",
            [FromQuery] bool? isActive = null)
        {
            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u =>
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search)  ||
                    u.Email.Contains(search));

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role == role);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => ToDto(u))
                .ToListAsync();

            return Ok(new PagedResult<UserDto>(items, total, page, pageSize));
        }

        // GET api/users/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            return user is null ? NotFound() : Ok(ToDto(user));
        }

       // POST api/users
[HttpPost]
[Authorize(Roles = "Admin")]  // ← seulement cette ligne change
public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
{
    if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLowerInvariant()))
        return Conflict(new { message = "Email déjà utilisé" });

    var user = new User
    {
        FirstName    = dto.FirstName,
        LastName     = dto.LastName,
        Email        = dto.Email.ToLowerInvariant(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role         = dto.Role,
        Phone        = dto.Phone,
        Department   = dto.Department
    };

    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    _userCreatedCounter.Inc();

    _log.LogInformation("Utilisateur créé par Admin : {Email}", user.Email);

    return CreatedAtAction(nameof(GetById), new { id = user.Id }, ToDto(user));
}

        // PUT api/users/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();

            user.FirstName  = dto.FirstName;
            user.LastName   = dto.LastName;
            user.Phone      = dto.Phone;
            user.Department = dto.Department;
            user.AvatarUrl  = dto.AvatarUrl;
            user.UpdatedAt  = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(ToDto(user));
        }

        // PATCH api/users/{id}/role
        [HttpPatch("{id:guid}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetRole(Guid id, SetRoleDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();

            user.Role      = dto.Role;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(ToDto(user));
        }

        // PATCH api/users/{id}/toggle-active
        [HttpPatch("{id:guid}/toggle-active")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();

            user.IsActive  = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/users/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            _log.LogInformation("Utilisateur supprimé : {Id}", id);
            return NoContent();
        }

        // GET api/users/stats
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Stats()
        {
            var total    = await _db.Users.CountAsync();
            var active   = await _db.Users.CountAsync(u => u.IsActive);
            var admins   = await _db.Users.CountAsync(u => u.Role == "Admin");
            var today    = await _db.Users.CountAsync(u =>
                u.CreatedAt.Date == DateTime.UtcNow.Date);

            return Ok(new
            {
                total,
                active,
                inactive = total - active,
                admins,
                newToday = today
            });
        }

        // ── Méthode privée ────────────────────────────────────────────────
        private static UserDto ToDto(User u) => new(
            u.Id, u.FirstName, u.LastName, u.Email,
            u.Role, u.IsActive, u.AvatarUrl,
            u.Phone, u.Department, u.CreatedAt
        );
    }
}