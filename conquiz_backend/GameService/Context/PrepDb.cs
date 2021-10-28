using GameService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Context
{
    public static class PrepDb
    {
        public static void PrepMigration(IApplicationBuilder app, bool isProd)
        {
            using var serviceScope = app.ApplicationServices.CreateScope();

            if(isProd)
            {
                ApplyMigrations(serviceScope.ServiceProvider.GetService<IDbContextFactory<DefaultContext>>());
            }

            ValidateResources(
                serviceScope.ServiceProvider.GetService<IMapGeneratorService>(),
                serviceScope.ServiceProvider.GetService<IGameService>(),
                serviceScope.ServiceProvider.GetService<IQuestionService>());

            Console.WriteLine("--> Database prepared!");
        }

        private static void ApplyMigrations(IDbContextFactory<DefaultContext> contextFactory)
        {
            Console.WriteLine("--> Attempting to apply migrations...");
            try
            {
                using var context = contextFactory.CreateDbContext();

                context.Database.Migrate();

                Console.WriteLine("--> Migrations added");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not run migrations: {ex.Message}");
            }
        }

        private static void ValidateResources(IMapGeneratorService mapGeneratorService, IGameService gameService, IQuestionService questionService)
        {
            try
            {
                // Validate map
                mapGeneratorService.ValidateMap();

                // Cancel all "stuck" games
                gameService.CancelOngoingGames();

                // Validate questions
                questionService.AddDefaultQuestions();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"--> Could not validate resources: {ex.Message}");
            }
        }
    }
}
