using GameService.Models;
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
using GameService.MessageBus;
using GameService.Dtos;
using GameService.Context;
using GameService.Services.GameTimerServices;

namespace GameService.Controllers
{
    /// <summary>
    /// Example controller
    /// </summary>
    /// 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GameController : ControllerBase
    {
        private readonly IExampleService context;
        private readonly IHttpClientFactory clientFactory;
        private readonly IMessageBusClient messageBus;
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IStatisticsService statisticsService;

        public GameController(IExampleService _context, 
            IHttpClientFactory clientFactory, 
            IMessageBusClient messageBus, 
            IDbContextFactory<DefaultContext> contextFactory, 
            IStatisticsService statisticsService)
        {
            context = _context;
            this.clientFactory = clientFactory;
            this.messageBus = messageBus;
            this.contextFactory = contextFactory;
            this.statisticsService = statisticsService;
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
        public IActionResult DoSomething()
        {
            try
            {
                CommonTimerFunc.RequestCapitalQuestions(messageBus, 90000, new List<int>()
                {
                    1,5,2,3,12,65,
                });

                return Ok($"Game controller version 1.3");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var result = await statisticsService.GetUserGameStatistics();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        } 
    }
}
