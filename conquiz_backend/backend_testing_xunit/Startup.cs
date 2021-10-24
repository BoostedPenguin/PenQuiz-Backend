using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using conquiz_backend.Context;
using conquiz_backend.Models;
using conquiz_backend.Services;
using conquiz_backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.DependencyInjection;

namespace backend_testing_xunit
{
    public class Startup
    {
        private static IConfiguration Configuration;
        public static void InitConfiguration()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
        }


        public void ConfigureServices(IServiceCollection services)
        {
            InitConfiguration();

            services.AddHttpContextAccessor();

            services.AddHttpClient();

        }
    }
}
