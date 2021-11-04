using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestionService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Context
{
    public static class PrepDb
    {
        public static void PrepMigration(IApplicationBuilder app, bool isProduction)
        {
            using var serviceScope = app.ApplicationServices.CreateScope();

            var contextFactory = serviceScope.ServiceProvider.GetService<IDbContextFactory<DefaultContext>>();
            using var db = contextFactory.CreateDbContext();
            
            if(isProduction)
            {
                ApplyMigrations(db);
            }

            Seed(db);
        }

        private static void ApplyMigrations(DefaultContext context)
        {
            Console.WriteLine("--> Attempting to apply migrations...");
            try
            {
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not run migrations: {ex.Message}");
            }
        }
        private static void Seed(DefaultContext db)
        {
            // Questions
            var questions = new Questions[6]
            {
                new Questions("In what year was Bulgaria founded?", "681"),
                new Questions("In what year did World War 2 start?", "1939"),
                new Questions("In what year did World War 1 end?", "1918"),
                new Questions("In what year was the first case of COVID19 discovered?", "2019"),
                new Questions("When did Bulgaria join the European Union?", "2016"),
                new Questions("How much is the world population in November 2021", "7904393844"),
            };

            var result = questions.Where(x => !db.Questions.Any(y => y.Question == x.Question)).ToList();

            if(result.Count() != 0)
            {
                db.Questions.AddRange(result);
                db.SaveChanges();
            }
        }
    }
}
