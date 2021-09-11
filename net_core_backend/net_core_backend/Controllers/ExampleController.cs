using net_core_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using net_core_backend.Services;
using net_core_backend.ViewModel;
using AutoMapper;
using net_core_backend.Services.Interfaces;

namespace net_core_backend.Controllers
{
    /// <summary>
    /// Example controller
    /// </summary>
    /// 
    [ApiController]
    [Route("api/[controller]")]
    public class ExampleController : ControllerBase
    {
        private readonly ILogger<ExampleController> _logger;
        private readonly IMapper mapper;
        private readonly IExampleService context;

        public ExampleController(ILogger<ExampleController> logger,IMapper mapper, IExampleService _context)
        {
            _logger = logger;
            this.mapper = mapper;
            context = _context;
        }


        [HttpGet("{word}")]
        public async Task<IActionResult> AddSomething([FromRoute] string word)
        {
            try
            {
                await context.DoSomething();

                var result = new DefaultModel() { Id = 5 };

                var dto = mapper.Map<ExampleViewModel>(result);

                return Ok("Scaffolding worked");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
