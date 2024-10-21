// ITokenService.cs
using AuthSystem.Models;

namespace AuthSystem.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
        bool ValidateToken(string token);
        string GetEmailFromToken(string token);
    }
}