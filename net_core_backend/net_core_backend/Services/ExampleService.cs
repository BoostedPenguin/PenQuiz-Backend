using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using net_core_backend.Context;
using net_core_backend.Models;
using net_core_backend.Services.Interfaces;
using net_core_backend.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace net_core_backend.Services
{
    public class ExampleService : DataService<DefaultModel>, IExampleService
    {
        private readonly IContextFactory contextFactory;
        private readonly IHttpContextAccessor httpContext;
        private readonly IMapGeneratorService mapGeneratorService;

        public ExampleService(IContextFactory _contextFactory, IHttpContextAccessor httpContextAccessor, IMapGeneratorService mapGeneratorService) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            httpContext = httpContextAccessor;
            this.mapGeneratorService = mapGeneratorService;
        }

        public async Task DoSomething()
        {
            var borders = await mapGeneratorService.GetBorders("srg");

            var g = 5;
        }
    }
}
