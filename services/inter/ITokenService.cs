using GestionProduits.Api.Models;
using System.Security.Claims;

namespace GestionProduits.Api.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);

        string GenerateRefreshToken();
        
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
