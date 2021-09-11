using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using net_core_backend.Context;
using net_core_backend.Models;
using net_core_backend.Profiles;
using net_core_backend.Services;
using net_core_backend.Services.Interfaces;
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

            services.AddAutoMapper(c => c.AddProfile<AutoMapping>(), typeof(Startup));

            services.AddSingleton<IContextFactory>(new ContextFactoryTesting(Configuration.GetConnectionString("Testing_SQLCONNSTR_Database")));

            services.AddSingleton<IExampleService, ExampleService>();


            // Comment this if you don't want to seed the database
            // WARNING: It may cause unexpected database errors
            //DatabaseSeeder.Seed(new ContextFactory(Configuration.GetConnectionString("Testing_SQLCONNSTR_Database")));
        }
    }
}
