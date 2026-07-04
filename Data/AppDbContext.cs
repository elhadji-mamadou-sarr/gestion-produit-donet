// Data/AppDbContext.cs
using GestionProduits.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionProduits.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Produit> Produits { get; set; }
    
    public DbSet<User> Users { get; set; }
}
