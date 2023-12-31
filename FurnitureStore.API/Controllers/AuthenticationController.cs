﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using FurnitureStore.API.Configuration;
using FurnitureStore.Data;
using FurnitureStore.Shared;
using FurnitureStore.Shared.Auth;
using FurnitureStore.Shared.Common;
using FurnitureStore.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FurnitureStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        private readonly IEmailSender _emailSender;
        private readonly FurnitureStoreContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly ILogger<AuthenticationController> _logger;


        public AuthenticationController(UserManager<IdentityUser> userManager, IOptions<JwtConfig> jwtConfig,
            IEmailSender emailSender, FurnitureStoreContext context, TokenValidationParameters tokenValidationParameters, ILogger<AuthenticationController> logger)
        {
            _userManager = userManager;
            _jwtConfig = jwtConfig.Value;
            _emailSender = emailSender;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
            _logger = logger;
        }

        [HttpPost("Registration")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto request)
        {
            _logger.LogWarning("debug");
            if (!ModelState.IsValid) return BadRequest();

            //verify if email exist
            var emailExist = await _userManager.FindByEmailAsync(request.Email);
            if (emailExist != null)
                return BadRequest(
                    new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Email already exist"
                        }
                    });
            var user = new IdentityUser()
            {
                Email = request.Email,
                UserName = request.Email,
                EmailConfirmed = false
            };
            var isCreated = await _userManager.CreateAsync(user, request.Password);
            if (!isCreated.Succeeded)
            {
                var errors = new List<string>();
                errors.AddRange(isCreated.Errors.Select(error => error.Description));
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = errors
                });
            }

            //var token = GenerateToken(user);
            await SendVerificationEmail(user);
            return Ok(new AuthResult()
            {
                Result = true,
                //Token = token
            });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest();
            var userExisting = await _userManager.FindByEmailAsync(request.Email);
            if (userExisting == null)
                return BadRequest(
                    new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Payload"
                        }
                    });
            if (!userExisting.EmailConfirmed)
                return BadRequest(
                    new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Email needs to be confirmed"
                        }
                    });
            var checkUserAndPass = await _userManager.CheckPasswordAsync(userExisting, request.Password);
            if (!checkUserAndPass)
                return BadRequest(
                    new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Credentials"
                        }
                    });
            var token =  await GenerateTokenAsync(userExisting);
            return Ok(token);
        }
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (!ModelState.IsValid) return BadRequest(
                new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid parameters"
                    }
                });
            var token = await VerifyAndGenerateTokenAsync(tokenRequest);
            if(token == null) return BadRequest(
                new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid Token"
                    }
                });
            return Ok(token);

        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                return BadRequest(new AuthResult
                {
                    Errors = new List<string> { "Invalid email confirmation url" },
                    Result = false
                });
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            var status = result.Succeeded
                ? "Thank you for confirming your email"
                : "There has been an error confirming your email.";
            return Ok(status);
        }

        private async Task<AuthResult> GenerateTokenAsync(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), //id del token
                    new Claim(JwtRegisteredClaimNames.Iat,
                        DateTime.Now.ToUniversalTime().ToString()), //issueAt hora y dia que el token fue emitido

                }),
                Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTime),
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken =  jwtTokenHandler.WriteToken(token);

            var refresToken = new RefreshToken
            {
                JwtId = token.Id,
                Token = RandomGenerator.GenerateRandomString(23),
                AddedDate = DateTime.UtcNow,
                ExpiryTime = DateTime.UtcNow.AddMonths(1),
                IsRevoked = false,
                IsUsed = false,
                UserId = user.Id
            };
            await _context.RefreshTokens.AddAsync(refresToken);
            await _context.SaveChangesAsync();
            return new AuthResult
            {
                Token = jwtToken,
                RefreshToken = refresToken.Token,
                Result = true
            };

        }

        private async Task SendVerificationEmail(IdentityUser user)
        {
            var vereficationCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            vereficationCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(vereficationCode));
            //example: https://web/api/authentication/ConfirmEmail/userId=exampleUserid&code=examplecode
            var callbackUrl =
                $@"{Request.Scheme}://{Request.Host}{Url.Action("ConfirmEmail", controller: "Authentication",
                    new { userid = user.Id, code = vereficationCode })}"; //Request y Url es codigo heredado
            var emailBody =
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'> Click here </a>";
            await _emailSender.SendEmailAsync(user.Email, "Confirm your email", emailBody);
        }

        private async Task<AuthResult> VerifyAndGenerateTokenAsync(TokenRequest tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                _tokenValidationParameters.ValidateLifetime = false;

                var tokenBeingVerified = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParameters, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase);

                    if (!result || tokenBeingVerified == null)
                        throw new Exception("Invalid Token");
                }

                var utcExpiryDate = long.Parse(tokenBeingVerified.Claims.
                    FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp).Value);

                var expiryDate = DateTimeOffset.FromUnixTimeSeconds(utcExpiryDate).UtcDateTime;
                if (expiryDate < DateTime.UtcNow)
                    throw new Exception("Token Expired");

                var storedToken = await _context.RefreshTokens.
                    FirstOrDefaultAsync(t => t.Token == tokenRequest.RefreshToken);
                if (storedToken == null)
                    throw new Exception("Invalid Token");

                if (storedToken.IsUsed || storedToken.IsRevoked)
                    throw new Exception("Invalid Token");

                var jti = tokenBeingVerified.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

                if (jti != storedToken.JwtId)
                    throw new Exception("Invalid Token");

                if (storedToken.ExpiryTime < DateTime.UtcNow)
                    throw new Exception("Token Expired");

                storedToken.IsUsed = true;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();

                var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);

                return await GenerateTokenAsync(dbUser);
            }
            catch (Exception ex)
            {
                var message = ex.Message == "Invalid Token" || ex.Message == "Token Expired"
                    ? ex.Message
                    : "Internal Server Error";
                return new AuthResult{
                    Result = false,
                    Errors = new List<string>()
                        {
                            "Invalid Token"
                        }
                    };

            }
        }
    }
}
