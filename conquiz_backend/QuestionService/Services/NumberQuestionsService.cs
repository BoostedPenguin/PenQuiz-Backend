using Microsoft.EntityFrameworkCore;
using QuestionService.Context;
using QuestionService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Services
{
    public interface INumberQuestionsService
    {
        Task<List<Questions>> GetNumberQuestions(List<int> amountRoundId, string sessionId, int gameInstanceId);
    }

    public class NumberQuestionsService : INumberQuestionsService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly Random random = new Random();
        public NumberQuestionsService(IDbContextFactory<DefaultContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<List<Questions>> GetNumberQuestions(List<int> amountRoundId, string sessionId, int gameInstanceId)
        {
            using var db = contextFactory.CreateDbContext();

            var questions = await db.Questions
                .Include(x => x.Answers)
                .Include(x => x.GameSessionQuestions)
                .ThenInclude(x => x.GameInstance)
                .Where(x => x.Type == "number" && !x.GameSessionQuestions
                    .Any(y => y.GameInstance.OpentDbSessionToken == sessionId))
                .ToListAsync();

            // If questions have been exchausted for this game session, repeat previous ones
            if (questions.Count() < amountRoundId.Count())
            {
                questions = await db.Questions
                    .Include(x => x.Answers)
                    .Where(x => x.Type == "number")
                    .ToListAsync();
            }


            var response = new List<Questions>();

            var currentRoundIndex = 0;
            // If questions in db will be enough
            if(questions.Count() <= amountRoundId.Count())
            {
                questions.ForEach(x => x.RoundId = amountRoundId[currentRoundIndex++]);
                response.AddRange(questions);
            }
            else
            {
                // Get randomized random questions
                while (response.Count() < amountRoundId.Count())
                {
                    var index = random.Next(0, questions.Count());

                    if (response.Contains(questions[index])) continue;

                    questions[index].RoundId = amountRoundId[currentRoundIndex++];
                    response.Add(questions[index]);
                }
            }

            foreach(var q in response)
            {
                await db.AddAsync(new GameSessionQuestions()
                {
                    GameInstanceId = gameInstanceId,
                    QuestionId = q.Id,
                });
            }

            await db.SaveChangesAsync();


            return response;
        }
    }
}
