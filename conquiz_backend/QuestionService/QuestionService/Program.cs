using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuestionService.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace QuestionService
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

                            Console.WriteLine($"--> Attempting to connect with provider: {provider}");

                            services.AddDbContextFactory<DefaultContext>(
                                options => _ = provider switch
                                {
                                    "Npgsql" => options.UseNpgsql(configuration.GetConnectionString("QuestionsConnNpgsql"),
                                x => x.MigrationsAssembly("QuestionService.NpgsqlMigrations")),

                                    "SqlServer" => options.UseSqlServer(
                                        configuration.GetConnectionString("QuestionsConn"),
                                        x => x.MigrationsAssembly("QuestionService.SqlServerMigrations")),

                                    _ => throw new Exception($"Unsupported provider: {provider}")
                                });
                        });
                });
    }
}
