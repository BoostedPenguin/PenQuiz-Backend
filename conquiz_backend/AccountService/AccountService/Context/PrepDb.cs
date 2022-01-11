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
        public static void PrepMigration(IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices.CreateScope();
            ApplyMigrations(serviceScope.ServiceProvider.GetService<IDbContextFactory<AppDbContext>>());
        }

        private static void ApplyMigrations(IDbContextFactory<AppDbContext> contextFactory)
        {
            using var context = contextFactory.CreateDbContext();
            var pendingMigrationCount = context.Database.GetPendingMigrations().Count();

            if (pendingMigrationCount != 0)
            {
                Console.WriteLine($"--> Attempting to apply {pendingMigrationCount} migrations...");

                context.Database.Migrate();

                Console.WriteLine($"--> Successfully applied all pending migrations");
            }
        }
    }
}
