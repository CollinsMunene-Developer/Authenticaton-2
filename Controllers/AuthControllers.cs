using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthSystem.Models;
using AuthSystem.Services;

namespace AuthSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        public AuthController(IAuthService authService, IEmailService emailService)
        {
            _authService = authService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterUserAsync(request);
                
                // Send verification email
                var verificationToken = await _authService.GenerateEmailVerificationTokenAsync(request.Email);
                await _emailService.SendVerificationEmailAsync(request.Email, verificationToken);
                
                return Ok(new { message = "Registration successful. Please check your email to verify your account." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                
                if (response.RequiresTwoFactor)
                {
                    return Ok(new { requiresTwoFactor = true, message = "2FA code required" });
                }

                if (!response.Success)
                {
                    return BadRequest(new { message = response.Message });
                }

                return Ok(new 
                { 
                    token = response.Token,
                    refreshToken = response.RefreshToken,
                    message = "Login successful"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
        {
            try
            {
                var result = await _authService.VerifyEmailAsync(request.Email, request.Token);
                if (result)
                {
                    return Ok(new { message = "Email verified successfully" });
                }
                return BadRequest(new { message = "Invalid verification token" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var resetToken = await _authService.GeneratePasswordResetTokenAsync(request.Email);
                await _emailService.SendPasswordResetEmailAsync(request.Email, resetToken);
                
                return Ok(new { message = "If the email exists, a password reset link has been sent." });
            }
            catch (Exception ex)
            {
                // Return the same message even if email doesn't exist (security best practice)
                return Ok(new { message = "If the email exists, a password reset link has been sent." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordConfirmation request)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(request);
                if (result)
                {
                    return Ok(new { message = "Password reset successful" });
                }
                return BadRequest(new { message = "Invalid reset token" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("2fa/setup")]
        public async Task<IActionResult> SetupTwoFactor()
        {
            try
            {
                var userEmail = User.Identity.Name; // Assuming email is stored in the Name claim
                var response = await _authService.SetupTwoFactorAsync(userEmail);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("2fa/verify")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerificationRequest request)
        {
            try
            {
                var userEmail = User.Identity.Name;
                var result = await _authService.VerifyTwoFactorSetupAsync(userEmail, request.Code);
                
                if (result)
                {
                    return Ok(new { message = "2FA setup verified successfully" });
                }
                return BadRequest(new { message = "Invalid verification code" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(request.Token, request.RefreshToken);
                
                if (response.Success)
                {
                    return Ok(new 
                    { 
                        token = response.Token,
                        refreshToken = response.RefreshToken
                    });
                }
                
                return BadRequest(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userEmail = User.Identity.Name;
                await _authService.LogoutAsync(userEmail);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}