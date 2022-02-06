﻿using AccountService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AccountService.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [Route("api/[controller]")]
    public class AccountAdminController : ControllerBase
    {
        private readonly IAccountAdminService adminService;
        private readonly IHttpClientFactory clientFactory;
        private readonly IOptions<AppSettings> appSettings;

        public AccountAdminController(IAccountAdminService adminService, IHttpClientFactory clientFactory, IOptions<AppSettings> appSettings)
        {
            this.adminService = adminService;
            this.clientFactory = clientFactory;
            this.appSettings = appSettings;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnverifiedQuestions([FromQuery] int pageNumber, [FromQuery] int pageEntries)
        {
            try
            {
                var questions = await adminService.GetAccounts(pageNumber, pageEntries);

                return Ok(questions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
