using FurnitureStore.API.Configuration;
using FurnitureStore.Shared.Auth;
using FurnitureStore.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest();

            //verify if email exist
            var emailExist = await _userManager.FindByEmailAsync(request.EmailAddress);
            if (emailExist == null) return BadRequest(
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
                Email = request.EmailAddress,
                UserName = request.EmailAddress
            };
            var isCreated= await _userManager.CreateAsync(user);
            if (!isCreated.Succeeded)
            {
                var errors = new List<string>();
                errors.AddRange((IEnumerable<string>)isCreated.Errors);
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
    }
}
