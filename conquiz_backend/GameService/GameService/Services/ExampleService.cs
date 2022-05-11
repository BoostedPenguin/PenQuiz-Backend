﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using GameService.Services.Interfaces;
using GameService.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using GameService.Data.Models;
using GameService.Data;

namespace GameService.Services
{
    public class ExampleService : IExampleService
    {

        public ExampleService(IDbContextFactory<DefaultContext> _contextFactory)
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
