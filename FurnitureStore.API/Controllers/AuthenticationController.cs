using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FurnitureStore.API.Configuration;
using FurnitureStore.Shared.Auth;
using FurnitureStore.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public AuthenticationController(UserManager<IdentityUser> userManager, IOptions<JwtConfig> jwtConfig)
        {
            _userManager = userManager;
            _jwtConfig = jwtConfig.Value;
        }

        [HttpPost("Registration")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest();

            //verify if email exist
            var emailExist = await _userManager.FindByEmailAsync(request.Email);
            if (emailExist != null) return BadRequest(
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
                UserName = request.Email
            };
            var isCreated= await _userManager.CreateAsync(user,request.Password);
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
            var token = GenerateToken(user);
            return Ok(new AuthResult()
            {
                Result = true,
                Token = token
            });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest();
            var userExisting = await _userManager.FindByEmailAsync(request.Email);
            if (userExisting == null) return BadRequest(
                new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid Payload"
                    }
                });
            var checkUserAndPass = await _userManager.CheckPasswordAsync(userExisting, request.Password);
            if(!checkUserAndPass)return BadRequest(
                new AuthResult(){
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid Credentials"
                    }
                });
            var token = GenerateToken(userExisting);
            return Ok(new AuthResult()
            {
                Result = true,
                Token = token
            });
        }
        private string GenerateToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id",user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub,user.Email),
                    new Claim(JwtRegisteredClaimNames.Email,user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()), //id del token
                    new Claim(JwtRegisteredClaimNames.Iat,DateTime.Now.ToUniversalTime().ToString()), //issueAt hora y dia que el token fue emitido

                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),SecurityAlgorithms.HmacSha256)
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }
    }
}
