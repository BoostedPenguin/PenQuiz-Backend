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
            var questions = new List<Questions>()
            {
                new Questions("In what year was Bulgaria founded?", "681"),
                new Questions("In what year did World War 2 start?", "1939"),
                new Questions("In what year did World War 1 end?", "1918"),
                new Questions("In what year was the first case of COVID19 discovered?", "2019"),
                new Questions("When did Bulgaria join the European Union?", "2007"),
                new Questions("How much is the world population in November 2021", "7904393844"),
                new Questions("What year was the very first model of the iPhone released?", "2007"),
                new Questions("What year did the Titanic movie come out?", "1997"),
                new Questions("How many parts (screws and bolts included) does the average car have?", "30000"),
                new Questions("About how many taste buds does the average human tongue have?", "10000"),
                new Questions("What percentage of our bodies is made up of water?", "63"),
                new Questions("How many times does the heartbeat per day?", "100000"),
                new Questions("In which year World War I begin?", "1914"),
                new Questions("When was the company Nike founded?", "1971"),
                new Questions("When did construction on the Empire State building started?", "1929"),
                new Questions("How many eyes does a bee have?", "5"),
                new Questions("In what year did Steve Jobs die?", "2011"),
                new Questions("How many cards are there in a deck of Uno?", "108"),
                new Questions("How many books are in the Catholic Bible?", "73"),
                new Questions("How many ribs are in a human body?", "24"),
                new Questions("Water has a pH level of around?", "7"),
                new Questions("On a dartboard, what number is directly opposite No. 1?", "19"),
                new Questions("How many colors are there in a rainbow?", "7"),
                new Questions("How long is an Olympic swimming pool (in meters)?", "50"),
                new Questions("Demolition of the Berlin wall began in what year?", "1989"),
                new Questions("How many hearts does an octopus have?", "3"),
                new Questions("How many legs does a spider have?", "8"),
                new Questions("How many months do elephant pregnancies last?", "22"),
                new Questions("How many teeth does an adult human have?", "32"),
                new Questions("How many inches are in a foot?", "12"),
                new Questions("How many zeros are in a million?", "6"),
                new Questions("How many oceans are there?", "4"),
                new Questions("What year was Minecraft released?", "2011"),
                new Questions("When was the U.S. established?", "1776"),
                new Questions("How many states did the U.S. start with?", "13"),
                new Questions("How many Earths can fit inside the sun?", "1300000"),
                new Questions("In which year was Notorious BIG (rapper) shot and killed?", "1997"),
                new Questions("In which year was Tupac Shakur shot and killed?", "1996"),
                new Questions("The British Raj lasted how many years in India?", "90"),
                new Questions("When did the Eiffel Tower open?", "1889"),
                new Questions("In which year before Christ was Julius Caesar assassinated by Roman senators?", "44"),
                new Questions("How many years ago were horses domesticated?", "6000"),
                new Questions("When did the USSR dissolve?", "1991"),
            };

            var result = questions.Where(x => !db.Questions.Any(y => y.Question.ToLower() == x.Question.ToLower())).ToList();

            if(result.Count() != 0)
            {
                db.Questions.AddRange(result);
                db.SaveChanges();
            }
        }
    }
}
