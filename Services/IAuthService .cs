// IAuthService.cs
using System.Threading.Tasks;
using AuthSystem.Models;

namespace AuthSystem.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterUserAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<string> GenerateEmailVerificationTokenAsync(string email);
        Task<bool> VerifyEmailAsync(string email, string token);
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordConfirmation request);
        Task<TwoFactorSetupResponse> SetupTwoFactorAsync(string email);
        Task<bool> VerifyTwoFactorSetupAsync(string email, string code);
        Task<LoginResponse> RefreshTokenAsync(string token, string refreshToken);
        Task LogoutAsync(string email);
    }
}

// AuthService.cs
using System;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AuthSystem.Models;
using AuthSystem.Data;

namespace AuthSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(
            ApplicationDbContext context,
            ITokenService tokenService,
            ITwoFactorService twoFactorService,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _tokenService = tokenService;
            _twoFactorService = twoFactorService;
            _passwordHasher = passwordHasher;
        }

        public async Task<bool> RegisterUserAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new Exception("Email already registered");
            }

            var (hash, salt) = _passwordHasher.HashPassword(request.Password);

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return new LoginResponse { Success = false, Message = "Invalid credentials" };
            }

            if (!user.IsEmailVerified)
            {
                return new LoginResponse { Success = false, Message = "Please verify your email first" };
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return new LoginResponse { Success = false, Message = "Invalid credentials" };
            }

            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrEmpty(request.TwoFactorCode))
                {
                    return new LoginResponse { RequiresTwoFactor = true };
                }

                if (!await _twoFactorService.ValidateCodeAsync(user.Email, request.TwoFactorCode))
                {
                    return new LoginResponse { Success = false, Message = "Invalid 2FA code" };
                }
            }

            var token = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new LoginResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken
            };
        }

        // Other interface implementations...
        // (Implementation details for other methods would follow similar patterns)
    }
}