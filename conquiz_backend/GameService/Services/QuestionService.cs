using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using GameService.Models;
using GameService.Services.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services
{
    public interface IQuestionService
    {
        Task<Questions> AnswerQuestion(int selectedAnswer, int questionId);
    }

    public class QuestionService : DataService<DefaultModel>, IQuestionService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        const string defaultQuestionsFile = "questions";

        public QuestionService(IDbContextFactory<DefaultContext> _contextFactory, IHttpContextAccessor httpContextAccessor) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task AddDefaultQuestions()
        {
            using var a = contextFactory.CreateDbContext();
            using StreamReader r = new StreamReader($"{defaultQuestionsFile}.json");

            string json = r.ReadToEnd();
            var defaultQuestions = JsonConvert.DeserializeObject<List<Questions>>(json);

            var questions = await a.Questions.Include(x => x.Answers).ToListAsync();

            var defaultQuestionsMissing = defaultQuestions
                .Select(x => x.Question)
                .Except(questions.Select(x => x.Question))
                .ToList();

            if (defaultQuestionsMissing.Count > 0)
            {
                await a.AddRangeAsync(defaultQuestions.Where(x => defaultQuestionsMissing.Contains(x.Question)).ToList());
                await a.SaveChangesAsync();
            }

            Console.WriteLine("Default questions validated.");
        }

        public async Task<Questions> AnswerQuestion(int selectedAnswer, int questionId)
        {
            using var a = contextFactory.CreateDbContext();

            var questionAnswers = await a.Questions
                .Include(x => x.Answers)
                .FirstOrDefaultAsync(x => x.Id == questionId);

            if (questionAnswers == null)
                throw new ArgumentException("There isn't a question with that id in our database");

            var answer = questionAnswers.Answers.FirstOrDefault(x => x.Id == selectedAnswer);
            
            // TODO enter if the user answered correctly or falsely inside of roundquestions
            // Either give / or not give him a territory
            // If you give him update his boundries
            // If you don't give him remove "attackedBy" from territory || make it neutral
            // Update his score
            return questionAnswers;
        }
    }
}
