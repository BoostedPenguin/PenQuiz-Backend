﻿using GameService.Data;
using GameService.Data.Models;
using GameService.Grpc;
using GameService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Context
{
    public static class PrepDb
    {
        public static void PrepMigration(IApplicationBuilder app, bool isProduction)
        {
            using var serviceScope = app.ApplicationServices.CreateScope();

            var logger = serviceScope.ServiceProvider.GetService<ILogger<DefaultContext>>();

            var contextFactory = serviceScope.ServiceProvider.GetService<IDbContextFactory<DefaultContext>>();
            using var db = contextFactory.CreateDbContext();

            if (!db.Database.IsInMemory())
            {
                ApplyMigrations(db, logger);
            }

            _ = ValidateResources(
                db,
                serviceScope.ServiceProvider.GetService<IMapGeneratorService>(),
                serviceScope.ServiceProvider.GetService<IGameService>(), logger);


            logger.LogInformation("Database prepared!");

            var grpcClient = serviceScope.ServiceProvider.GetService<IAccountDataClient>();

            var users = grpcClient.ReturnAllAccounts();

            if (users == null) return;
            FetchAccounts(contextFactory, users, logger);
        }

        private static void FetchAccounts(IDbContextFactory<DefaultContext> contextFactory, IEnumerable<Users> users, ILogger logger = null)
        {
            logger.LogInformation("Applying missing users...");

            using var db = contextFactory.CreateDbContext();

            foreach(var user in users)
            {
                if(db.Users.FirstOrDefault(x => x.UserGlobalIdentifier == user.UserGlobalIdentifier) == null)
                {
                    db.Users.Add(user);
                }
            }
            db.SaveChanges();
        }

        private static void ApplyMigrations(DefaultContext context, ILogger logger = null)
        {
            logger.LogInformation("Attempting to apply migrations...");
            
            context.Database.Migrate();

            logger.LogInformation("Migrations added");
        }

        private static async Task ValidateResources(DefaultContext db,IMapGeneratorService mapGeneratorService, IGameService gameService, ILogger logger = null)
        {
            try
            {
                // Validate map
                await mapGeneratorService.ValidateMap(db);

                // Cancel all "stuck" games
                await gameService.CancelOngoingGames(db);

                // Validate questions
                //questionService.AddDefaultQuestions();

                await MapGeneratorService.LoadDefaultMapBordersInMemory(db);
            }
            catch(Exception ex)
            {
                logger.LogError($"Could not validate resources: {ex.Message}");
            }
        }
    }
}
