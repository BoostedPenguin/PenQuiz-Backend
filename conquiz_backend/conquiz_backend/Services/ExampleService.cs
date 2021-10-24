using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using conquiz_backend.Context;
using conquiz_backend.Models;
using conquiz_backend.Services.Interfaces;
using conquiz_backend.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace conquiz_backend.Services
{
    public class ExampleService : DataService<DefaultModel>, IExampleService
    {
        private readonly IContextFactory contextFactory;

        public ExampleService(IContextFactory _contextFactory) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
        }

        public async Task<bool> DoSomething()
        {
            //var borders = await mapGeneratorService.GetBorders("srg", "ar");
            //var b = httpContext.GetCurrentUserId();

            return true;
        }
    }
}
