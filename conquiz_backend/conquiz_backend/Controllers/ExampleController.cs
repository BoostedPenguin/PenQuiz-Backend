using conquiz_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using conquiz_backend.Services;
using conquiz_backend.ViewModel;
using conquiz_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace conquiz_backend.Controllers
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

        public ExampleController(IExampleService _context)
        {
            context = _context;
        }

        [HttpGet("{word}")]
        public async Task<IActionResult> AddSomething([FromRoute] string word)
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
