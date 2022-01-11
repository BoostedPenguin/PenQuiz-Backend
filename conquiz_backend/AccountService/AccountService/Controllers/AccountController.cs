using AccountService.Data.Models.Requests;
using AccountService.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AccountService.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService accountService;
        private readonly IHttpClientFactory clientFactory;
        private readonly IOptions<AppSettings> appSettings;

        public AccountController(IAccountService accountService, IHttpClientFactory clientFactory, IOptions<AppSettings> appSettings)
        {
            this.accountService = accountService;
            this.clientFactory = clientFactory;
            this.appSettings = appSettings;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ExamplePing()
        {
            try
            {
                await PingRequiredServices();

                return Ok("Successfully contacted me. Version 1.4");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] UserView userView)
        {
            try
            {
                await PingRequiredServices();

                var payload = GoogleJsonWebSignature.ValidateAsync(userView.TokenId, new GoogleJsonWebSignature.ValidationSettings()).Result;
                var response = await accountService.Authenticate(payload, ipAddress());

                if (response == null)
                    return BadRequest(new { message = "Validation failed." });

                setTokenCookie(response.RefreshToken);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Azure app services free plan sleeps idled services. Wake them up on authentication
        /// </summary>
        /// <returns></returns>
        private async Task PingRequiredServices()
        {
            // Activate only in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != Environments.Production)
                return;

            // Activate only in azure production
            if (appSettings.Value.AzureProduction == "")
                return;

            var client = clientFactory.CreateClient();
            using var questionServerResponse = await client.GetAsync($"https://conquiz-question-api.azurewebsites.net/api/question");
            using var gameServerResponse = await client.GetAsync($"https://conquiz-game-api.azurewebsites.net/api/game");

            if (!questionServerResponse.IsSuccessStatusCode)
                throw new ArgumentException("Our question server is down. Please, try again later.");

            if (!gameServerResponse.IsSuccessStatusCode)
                throw new ArgumentException("Our game server is down. Please, try again later.");
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                await PingRequiredServices();

                var refreshToken = Request.Cookies["refreshToken"];

                if (refreshToken == null)
                    return Unauthorized(new { message = "No refresh cookie received" });
                var response = await accountService.RefreshToken(refreshToken, ipAddress());

                if (response == null)
                    return Unauthorized(new { message = "Invalid token" });

                setTokenCookie(response.RefreshToken);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("revoke-cookie")]
        public async Task<IActionResult> RevokeCookie()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                if(refreshToken == null)
                {
                    return BadRequest(new { message = "There wasn't a refresh token cookie in the request" });
                }

                var status = await accountService.RevokeCookie(refreshToken, ipAddress());

                if(status)
                {
                    return Ok();
                }
                else
                {
                    return BadRequest(new { message = "The cookie is already inactive" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            var response = await accountService.RevokeToken(token, ipAddress());

            if (!response)
                return NotFound(new { message = "Token not found" });

            return Ok(new { message = "Token revoked" });
        }

        private void setTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.None,
                Secure = true,
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}
