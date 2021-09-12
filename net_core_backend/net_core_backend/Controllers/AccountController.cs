using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using net_core_backend.Helpers;
using net_core_backend.Models;
using net_core_backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace net_core_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService accountService;
        private readonly AppSettings _appSettings;

        public AccountController(IAccountService accountService, IOptions<AppSettings> appSettings)
        {
            this.accountService = accountService;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> Google([FromBody] UserView userView)
        {
            try
            {
                var payload = GoogleJsonWebSignature.ValidateAsync(userView.TokenId, new GoogleJsonWebSignature.ValidationSettings()).Result;
                var user = await accountService.Authenticate(payload);

                var claims = new[]
                {
                    //new Claim(JwtRegisteredClaimNames.Sub, Security.Encrypt(_appSettings.EmailSecret, user.Email)),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSettings.Secret));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(string.Empty,
                  string.Empty,
                  claims,
                  expires: DateTime.Now.AddSeconds(55 * 60),
                  signingCredentials: creds);
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token)
                });
            }
            catch (Exception ex)
            {
                BadRequest(ex.Message);
            }
            return BadRequest();
        }

    }
}
