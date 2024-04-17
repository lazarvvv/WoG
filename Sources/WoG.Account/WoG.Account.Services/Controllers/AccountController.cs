using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WoG.Accounts.Services.Api.Models;
using WoG.Accounts.Services.Api.Models.Dtos;
using WoG.Accounts.Services.Api.Records;

namespace WoG.Accounts.Services.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private IConfiguration appSettings;

        public AccountController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration appSettings)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.appSettings = appSettings;
        }

        [HttpPost("register")]
        public async Task<ObjectResult> Register([FromBody] RegisterDto registerDto)
        {
            if (registerDto is null)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, "Model is empty");
            }

            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, "Passwords do not match");
            }

            var newUser = new IdentityUser()
            {
                Id = Guid.NewGuid().ToString(),
                UserName = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = registerDto.Password
            };

            var user = await userManager.FindByEmailAsync(newUser.Email);

            if (user is not null)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, $"User with e-mail {newUser.Email} already exists.");
            }

            var createUser = await userManager.CreateAsync(newUser!, registerDto.Password);

            if (!createUser.Succeeded)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, $"Registration failed, please try again.");
            }
            
            var checkAdmin = await roleManager.FindByNameAsync("GM");

            if (checkAdmin is null)
            {
                await roleManager.CreateAsync(new IdentityRole() { Name = "GM" });
                await userManager.AddToRoleAsync(newUser, "GM");
                return this.StatusCode(StatusCodes.Status200OK, newUser);
            }

            var checkUser = await roleManager.FindByNameAsync("User");
            if (checkUser is null)
            {
                await roleManager.CreateAsync(new IdentityRole() { Name = "User" });
            }

            await userManager.AddToRoleAsync(newUser, "User");
            return this.StatusCode(StatusCodes.Status200OK, newUser);
        }

        [HttpPost("login")]
        public async Task<ObjectResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, "Model is empty");
            }

            var user = await userManager.FindByNameAsync(loginDto.Username);

            if (user is null)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, "Invalid email/password");
            }

            bool checkUserPasswords = await userManager.CheckPasswordAsync(user, loginDto.Password);

            if (!checkUserPasswords)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, "Invalid email/password");
            }

            var getUserRole = await userManager.GetRolesAsync(user);
            var userSession = new UserSession(user.Id, user.UserName, user.Email, getUserRole.First());
            string token = GenerateToken(userSession);

            return this.StatusCode(StatusCodes.Status200OK, token!);
        }

        private string GenerateToken(UserSession user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.GetValue<string>("Jwt:Key")!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id!),
                new Claim(ClaimTypes.Name, user.Username!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role!)
            };

            var token = new JwtSecurityToken(
                audience: appSettings.GetValue<string>("Jwt:Audience"),
                issuer: appSettings.GetValue<string>("Jwt:Issuer"),
                claims: userClaims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
