using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestionService.Data;
using QuestionService.Data.Models;
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

            if (isProduction)
            {
                ApplyMigrations(db);
            }


            Seed(db);
        }

        private static void ApplyMigrations(DefaultContext context)
        {
            Console.WriteLine("--> Attempting to apply migrations...");
            context.Database.Migrate();
            Console.WriteLine("--> Migrations added");
        }
        private static void Seed(DefaultContext db)
        {
            // Seed Questions
            var questions = new List<Questions>();
            questions.AddRange(SeedNumberQuestions());
            questions.AddRange(SeedMCQuestions());

            var result = questions.Where(x => !db.Questions.Any(y => y.Question.ToLower() == x.Question.ToLower())).ToList();

            if (result.Count != 0)
            {
                db.Questions.AddRange(result);
                db.SaveChanges();
            }
        }

        private static Questions[] SeedNumberQuestions()
        {
            return new Questions[]
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
        }

        private static Questions[] SeedMCQuestions()
        {
            return new Questions[]
            {
                new Questions("Which is the capital of Bulgaria?", "Sofia", new string[]
                {
                    "Burgas", "Plovdiv", "Varna"
                }),
                new Questions("Which of these European cities has the largest population?", "Istanbul", new string[]
                {
                    "Moscow", "London", "Varna"
                }),
                new Questions("What countries made up the original Axis powers in World War II?", "Germany, Italy, Japan", new string[]
                {
                    "Germany, Russia, Austria", "France, Germany, Russia", "France, Germany, Italy"
                }),
                new Questions("What is cynophobia?", "Fear of dogs", new string[]
                {
                    "Fear of insects", "Fear of shoes", "Fear of cats"
                }),
                new Questions("Which language is written from right to left?", "Arabic", new string[]
                {
                    "Spanish", "Chinese", "Bulgarian"
                }),
                new Questions("What is the name of the biggest technology company in South Korea?", "Samsung", new string[]
                {
                    "LG", "Sony", "Philips"
                }),
                new Questions("What is the name of the largest ocean on earth?", "Pacific", new string[]
                {
                    "Indian", "Atlantic", "Arctic"
                }),
                new Questions("Which country consumes the most chocolate per capita?", "Switzerland", new string[]
                {
                    "Finland", "Belgium", "Russia"
                }),
                new Questions("What was the first soft drink in space?", "Coca Cola", new string[]
                {
                    "Fanta", "Pepsi", "Sprite"
                }),
                new Questions("Which is the only edible food that never goes bad?", "Honey", new string[]
                {
                    "Rice", "Chocolate", "Vegetable Oil"
                }),
                new Questions("Which country invented ice cream?", "China", new string[]
                {
                    "Belgium", "Finland", "Russia"
                }),
                new Questions("What’s the shortcut for the 'copy' function on most computers?", "CTRL C", new string[]
                {
                    "CTRL X", "CTRL V", "ALT CTRL"
                }),
                new Questions("Which planet is the hottest in the solar system?", "Venus", new string[]
                {
                    "Mercury", "Mars", "Saturn"
                }),
                new Questions("Which natural disaster is measured with a Richter scale?", "Earthquakes", new string[]
                {
                    "Volcanos", "Tornados", "Tsunamis"
                }),
                new Questions("Which planet has the most gravity?", "Jupiter", new string[]
                {
                    "Saturn", "Earth", "Neptune"
                }),
                new Questions("How many Lord of the Rings films are there?", "3", new string[]
                {
                    "2", "4", "6"
                }),
                new Questions("What is the name of the thin and long country that spans more than half of the western coast of South America?", "Chile", new string[]
                {
                    "Peru", "Colombia", "Argentina"
                }),
                new Questions("Which two countries share the longest international border?", "Canada and the USA", new string[]
                {
                    "Russia and China", "Norway and Sweden", "Mexico and the USA"
                }),
                new Questions("Which continent is the largest?", "Asia", new string[]
                {
                    "Australia", "Africa", "South America"
                }),
                new Questions("By what name were the Egyptian kings/rulers known?", "Pharaohs", new string[]
                {
                    "Hans", "Kings", "Tsars"
                }),
                new Questions("Which religion dominated the Middle Ages?", "Catholicism", new string[]
                {
                    "Islam", "Buddhism", "Judaism"
                }),
                new Questions("In which country Adolph Hitler was born?", "Austria", new string[]
                {
                    "Germany", "Poland", "Netherlands"
                }),
                new Questions("Which country did AC/DC originate in?", "Australia", new string[]
                {
                    "England", "USA", "New Zealand"
                }),
                new Questions("Who was the messenger of the gods in Greek mythology?", "Hermes", new string[]
                {
                    "Zeus", "Dionysus", "Apollo"
                }),
                new Questions("The Roman God of War inspired the name of which planet?", "Mars", new string[]
                {
                    "Venus", "Jupiter", "Saturn"
                }),
                new Questions("What was the name of the Egyptian God of the Sun?", "Ra", new string[]
                {
                    "Osiris", "Seth", "Anubis"
                }),
            };
        }
    }
}
