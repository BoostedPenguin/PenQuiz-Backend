﻿using GameService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using GameService.Services;
using GameService.ViewModel;
using GameService.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.Http;

namespace GameService.Controllers
{
    /// <summary>
    /// Example controller
    /// </summary>
    /// 
    [ApiController]
    [Route("api/[controller]")]
    public class ExampleController : ControllerBase
    {
        private readonly IExampleService context;
        private readonly IHttpClientFactory clientFactory;

        public ExampleController(IExampleService _context, IHttpClientFactory clientFactory)
        {
            context = _context;
            this.clientFactory = clientFactory;
        }

        [HttpGet("contact")]
        public async Task<IActionResult> ContactAccount()
        {
            var url = "http://accounts-clusterip-srv:80/api/account";

            var client = clientFactory.CreateClient();

            try
            {
                using var response = await client.GetAsync(url);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DoSomething()
        {
            try
            {
                var result = await context.DoSomething();
                //await questionService.AddDefaultQuestions();
                //await gameService.CreateGameLobby();

                return Ok($"Did scaffolding work: {result}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
