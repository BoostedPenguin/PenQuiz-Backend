using AccountService.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Context
{
    public static class PrepDb
    {
        public static void PrepMigration(IApplicationBuilder app, bool isProduction)
        {
            using var serviceScope = app.ApplicationServices.CreateScope();

            var contextFactory = serviceScope.ServiceProvider.GetService<IDbContextFactory<AppDbContext>>();

            using var context = contextFactory.CreateDbContext();

            
            if (!context.Database.IsInMemory())
            {
                ApplyMigrations(context);
            }
        }

        private static void ApplyMigrations(AppDbContext contextFactory)
        {
            Console.WriteLine("--> Attempting to apply migrations...");
            contextFactory.Database.Migrate();

            Console.WriteLine("--> Migrations added");
        }
    }
}
