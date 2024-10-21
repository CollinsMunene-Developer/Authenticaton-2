using System;
using System.Threading.Tasks;
using AuthSystem.Models;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace AuthSystem.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly ApplicationDbContext _context;
        private const string Issuer = "YourApp";

        public TwoFactorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TwoFactorSetupResponse> GenerateSetupInfoAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            var secretKey = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secretKey);
            
            user.TwoFactorSecretKey = base32Secret;
            await _context.SaveChangesAsync();

            var qrCodeUrl = GenerateQrCodeUrl(email, base32Secret);

            return new TwoFactorSetupResponse
            {
                SecretKey = base32Secret,
                QrCodeUrl = qrCodeUrl,
                ManualEntryKey = base32Secret
            };
        }

        public string GenerateQrCodeUrl(string email, string secretKey)
        {
            return $"otpauth://totp/{Issuer}:{email}?secret={secretKey}&issuer={Issuer}";
        }

        public async Task<bool> ValidateCodeAsync(string email, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            {
                return false;
            }

            var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecretKey);
            var totp = new Totp(secretBytes);
            return totp.VerifyTotp(code, out long timeStepMatched);
        }
    }
}