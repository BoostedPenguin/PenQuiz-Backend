using AccountService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AccountService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().ConfigureServices(
                        (hostContext, services) =>
                        {
                            // Set the active provider via configuration
                            var configuration = hostContext.Configuration;

                            var provider = configuration.GetValue("Provider", "SqlServer");

                            if (hostContext.HostingEnvironment.IsProduction())
                            {
                                Console.WriteLine($"--> Using production database with provider: {provider}");
                                services.AddDbContextFactory<AppDbContext>(
                                    options => _ = provider switch
                                    {
                                        "Npgsql" => options.UseNpgsql(configuration.GetConnectionString("AccountsConnNpgsql"),
                                    x => x.MigrationsAssembly("AccountService.NpgsqlMigrations")),

                                        "SqlServer" => options.UseSqlServer(
                                            configuration.GetConnectionString("AccountsConn"),
                                            x => x.MigrationsAssembly("AccountService.SqlServerMigrations")),

                                        _ => throw new Exception($"Unsupported provider: {provider}")
                                    });
                            }
                            else
                            {
                                Console.WriteLine($"--> Using development database with provider: {provider}");

                                services.AddDbContextFactory<AppDbContext>(
                                    options => _ = provider switch
                                    {
                                        "Npgsql" => options.UseNpgsql(configuration.GetConnectionString("AccountsConnDevNpgsql"),
                                    x => x.MigrationsAssembly("AccountService.NpgsqlMigrations")),

                                        "SqlServer" => options.UseInMemoryDatabase("InMem"),

                                        _ => throw new Exception($"Unsupported provider: {provider}")
                                    });
                            }
                        });
                });
    }
}
