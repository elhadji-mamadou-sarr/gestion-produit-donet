using GestionProduits.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionProduits.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Produit> Produits => Set<Produit>();
}
