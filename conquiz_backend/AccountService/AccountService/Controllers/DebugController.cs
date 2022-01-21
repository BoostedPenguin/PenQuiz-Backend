using AccountService.Data.Models.Requests;
using AccountService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading.Tasks;

namespace AccountService.Controllers
{
    /// <summary>
    /// Used to generate debug jwt tokens
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly IOptions<AppSettings> options;
        private readonly IAccountService service;

        public DebugController(IOptions<AppSettings> options, IAccountService service)
        {
            this.options = options;
            this.service = service;
        }

        [AllowAnonymous]
        [HttpPost("issue-user")]
        public IActionResult IssueUserJwtToken(DebugTokenRequest request)
        {
            try
            {
                if (options.Value.DebugAccessToken == null || options.Value.DebugAccessToken.Length < 10)
                    throw new ArgumentException("System access token is less than 10 characters. Debug requests will be canceled.");

                if (request.AccessToken != options.Value.DebugAccessToken)
                    throw new ArgumentException("Invalid access token");
                
                return Ok(new
                {
                    jwt = service.IssueDebugJwtToken("user")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("issue-admin")]
        public IActionResult IssueAdminJwtToken(DebugTokenRequest request)
        {
            try
            {
                if (options.Value.DebugAccessToken.Length < 10)
                    throw new ArgumentException("System access token is less than 10 characters. Debug requests will be canceled.");

                if (request.AccessToken != options.Value.DebugAccessToken)
                    throw new ArgumentException("Invalid access token");

                return Ok(new
                {
                    jwt = service.IssueDebugJwtToken("admin")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
