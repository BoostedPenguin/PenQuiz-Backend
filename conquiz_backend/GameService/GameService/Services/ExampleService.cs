using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using GameService.Models;
using GameService.Services.Interfaces;
using GameService.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace GameService.Services
{
    public class ExampleService : DataService<DefaultModel>, IExampleService
    {

        public ExampleService(IDbContextFactory<DefaultContext> _contextFactory) : base(_contextFactory)
        {
        }

        public bool DoSomething()
        {
            //var borders = await mapGeneratorService.GetBorders("srg", "ar");
            //var b = httpContext.GetCurrentUserId();

            return true;
        }
    }
}
