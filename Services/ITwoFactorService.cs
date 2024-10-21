using System.Threading.Tasks;
using AuthSystem.Models;

namespace AuthSystem.Services
{
    public interface ITwoFactorService
    {
        Task<TwoFactorSetupResponse> GenerateSetupInfoAsync(string email);
        Task<bool> ValidateCodeAsync(string email, string code);
        string GenerateQrCodeUrl(string email, string secretKey);
    }
}