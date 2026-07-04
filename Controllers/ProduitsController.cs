// Controllers/ProduitsController.cs
using GestionProduits.Api.Data;
using GestionProduits.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;  // ← ajout

namespace GestionProduits.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProduitsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProduitsController> _logger;

    // ← ajout métriques
    private static readonly Counter _productCreatedCounter = Metrics
        .CreateCounter("products_created_total", "Nombre total de produits créés");

    private static readonly Counter _productDeletedCounter = Metrics
        .CreateCounter("products_deleted_total", "Nombre total de produits supprimés");

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
        _productCreatedCounter.Inc();  // ← ajout
        _logger.LogInformation("Produit créé : {Nom}", produit.Nom);
        return CreatedAtAction(nameof(GetById), new { id = produit.Id }, produit);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Produit produit)
    {
        var existingProduit = await _context.Produits.FindAsync(id);
        if (existingProduit == null) return NotFound();

        existingProduit.Nom = produit.Nom;
        existingProduit.Description = produit.Description;
        existingProduit.Prix = produit.Prix;
        existingProduit.Quantite = produit.Quantite;

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
        _productDeletedCounter.Inc();  // ← ajout
        _logger.LogInformation("Produit supprimé : {Id}", id);
        return NoContent();
    }
}
