using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;


namespace GameService
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

                                services.AddDbContextFactory<DefaultContext>(
                                    options => _ = provider switch
                                    {
                                        "Npgsql" => options.UseNpgsql(configuration.GetConnectionString("GamesConnNpgsql"),
                                    x => x.MigrationsAssembly("GameService.NpgsqlMigrations")),

                                        "SqlServer" => options.UseSqlServer(
                                            configuration.GetConnectionString("GamesConn"),
                                            x => x.MigrationsAssembly("GameService.SqlServerMigrations")),

                                        _ => throw new Exception($"Unsupported provider: {provider}")
                                    });
                            }
                            else
                            {
                                Console.WriteLine($"--> Using development database with provider: {provider}");

                                services.AddDbContextFactory<DefaultContext>(
                                    options => _ = provider switch
                                    {
                                        "Npgsql" => options.UseNpgsql(configuration.GetConnectionString("GamesConnNpgsql"),
                                    x => x.MigrationsAssembly("GameService.NpgsqlMigrations")),

                                        "SqlServer" => options.UseSqlServer(
                                            configuration.GetConnectionString("GamesConn"),
                                            x => x.MigrationsAssembly("GameService.SqlServerMigrations")),

                                        _ => throw new Exception($"Unsupported provider: {provider}")
                                    });
                            }
                        });
                });
    }
}
