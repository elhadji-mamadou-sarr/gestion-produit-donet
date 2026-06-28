using System.Security.Claims;
using BCrypt.Net;
using GestionProduits.Api.DTOs;
using GestionProduits.Api.Infrastructure;
using GestionProduits.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionProduits.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(AppDbContext db, ILogger<UsersController> log) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Profil personnel ────────────────────────────────────────────────

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();
        return ToDto(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileDto dto)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        user.Department = dto.Department;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToDto(user);
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mot de passe actuel incorrect" });
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Admin CRUD ──────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = db.Users.AsQueryable();
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => ToDto(u))
            .ToListAsync();
        return new PagedResult<UserDto>(items, total, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        return ToDto(user);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email.ToLowerInvariant()))
            return Conflict(new { message = "Email déjà utilisé" });

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            Phone = dto.Phone,
            Department = dto.Department
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        log.LogInformation("Utilisateur créé par Admin : {Email}", user.Email);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ToDto(user));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        user.Department = dto.Department;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToDto(user);
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetRole(Guid id, SetRoleDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.Role = dto.Role;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        log.LogWarning("Utilisateur supprimé : {Id}", id);
        return NoContent();
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Stats()
    {
        var total = await db.Users.CountAsync();
        var active = await db.Users.CountAsync(u => u.IsActive);
        var admins = await db.Users.CountAsync(u => u.Role == "Admin");
        return Ok(new { total, active, inactive = total - active, admins });
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.FirstName, u.LastName, u.Email,
        u.Role, u.IsActive, u.AvatarUrl, u.Phone, u.Department, u.CreatedAt);
}