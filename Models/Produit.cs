// Models/Produit.cs
using System.ComponentModel.DataAnnotations;

namespace GestionProduits.Api.Models;

public class Produit
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Prix { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantite { get; set; }
}