using System.Security.Claims;
using GestionProduits.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProduits.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private const string NotImplementedMessage = "Sera implémenté via Keycloak Admin API";

    private string CurrentUserSub =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!;

    private string CurrentUserEmail =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue("email")!;

    // ── Profil personnel ────────────────────────────────────────────────

    [HttpGet("me")]
    public IActionResult GetMe()
    {
        return Ok(new
        {
            sub = CurrentUserSub,
            email = CurrentUserEmail,
            firstName = User.FindFirstValue(ClaimTypes.GivenName),
            lastName = User.FindFirstValue(ClaimTypes.Surname),
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
        });
    }

    [HttpPut("me")]
    public IActionResult UpdateMe(UpdateProfileDto dto) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });

    [HttpPut("me/password")]
    public IActionResult ChangePassword(ChangePasswordDto dto) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });

    // ── Admin CRUD ──────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public IActionResult GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public IActionResult GetById(Guid id) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public IActionResult Create(CreateUserDto dto) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public IActionResult Update(Guid id, UpdateUserDto dto) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public IActionResult SetRole(Guid id, SetRoleDto dto) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });

    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public IActionResult ToggleActive(Guid id) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public IActionResult Delete(Guid id) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public IActionResult Stats() =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = NotImplementedMessage });
}
