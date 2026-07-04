// Controllers/ProduitsController.cs
using GestionProduits.Api.Infrastructure;
using GestionProduits.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace GestionProduits.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProduitsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProduitsController> _logger;

    public ProduitsController(AppDbContext context, ILogger<ProduitsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Produit>>> GetAll()
    {
        _logger.LogInformation("Récupération de tous les produits");
        return await _context.Produits.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Produit>> GetById(int id)
    {
        var produit = await _context.Produits.FindAsync(id);
        if (produit == null) return NotFound();
        return produit;
    }

    [HttpPost]
    public async Task<ActionResult<Produit>> Create(Produit produit)
    {
        _context.Produits.Add(produit);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Produit créé : {Nom}", produit.Nom);
        return CreatedAtAction(nameof(GetById), new { id = produit.Id }, produit);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Produit produit)
    {
        if (id != produit.Id) return BadRequest();
        _context.Entry(produit).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var produit = await _context.Produits.FindAsync(id);
        if (produit == null) return NotFound();
        _context.Produits.Remove(produit);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Produit supprimé : {Id}", id);
        return NoContent();
    }
}