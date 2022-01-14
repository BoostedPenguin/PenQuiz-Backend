using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Services
{
    public class LocalMCQuestionsService : IMCQuestionsService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly Random r = new();
        public LocalMCQuestionsService(IDbContextFactory<DefaultContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<SessionTokenRequest> GenerateSessionToken(string gameGlobalIdentifier)
        {
            // If there is an existing token return it without making a new one

            using var db = contextFactory.CreateDbContext();

            var gm = await db.GameInstances.FirstOrDefaultAsync(x => x.ExternalGlobalId == gameGlobalIdentifier);

            // Hasn't been added to db yet
            if (gm == null)
            {
                gm = new GameInstance()
                {
                    ExternalGlobalId = gameGlobalIdentifier,
                    OpentDbSessionToken = Guid.NewGuid().ToString(),
                };

                await db.AddAsync(gm);
                await db.SaveChangesAsync();
            }

            return new SessionTokenRequest()
            {
                Token = gm.OpentDbSessionToken,
                InternalGameInstanceId = gm.Id,
            };
        }

        public async Task<List<Questions>> GetMultipleChoiceQuestion(string sessionToken, List<int> multipleChoiceQuestions)
        {
            if (multipleChoiceQuestions.Count == 0)
                return new List<Questions>();

            using var db = contextFactory.CreateDbContext();

            var questions = await db.Questions
                .Include(x => x.Answers)
                .Include(x => x.GameSessionQuestions)
                .ThenInclude(x => x.GameInstance)
                .Where(x => x.Type == "multiple" && !x.GameSessionQuestions
                    .Any(y => y.GameInstance.OpentDbSessionToken == sessionToken))
                .ToListAsync();


            // If questions have been exchausted for this game session, repeat previous ones
            if (questions.Count() < multipleChoiceQuestions.Count())
            {
                questions = await db.Questions
                    .Include(x => x.Answers)
                    .Where(x => x.Type == "number")
                    .ToListAsync();
            }

            // No available questions in our system
            if (questions.Count == 0)
                throw new ArgumentException("No multiple choice questions in our system. Critical error.");


            var response = new List<Questions>();

            var currentRoundIndex = 0;
            // If questions in db will be enough
            if (questions.Count() <= multipleChoiceQuestions.Count())
            {
                questions.ForEach(x => x.RoundId = multipleChoiceQuestions[currentRoundIndex++]);
                response.AddRange(questions);
            }
            else
            {
                // Get randomized random questions
                while (response.Count() < multipleChoiceQuestions.Count())
                {
                    var index = r.Next(0, questions.Count());

                    if (response.Contains(questions[index])) continue;

                    questions[index].RoundId = multipleChoiceQuestions[currentRoundIndex++];
                    response.Add(questions[index]);
                }
            }

            var gm = db.GameInstances.FirstOrDefault(x => x.OpentDbSessionToken == sessionToken);

            if (gm == null)
                throw new ArgumentException($"Fatal error: no game instance in database with session token: {sessionToken}");

            foreach (var q in response)
            {
                await db.AddAsync(new GameSessionQuestions()
                {
                    GameInstanceId = gm.Id,
                    QuestionId = q.Id,
                });
            }

            await db.SaveChangesAsync();


            return response;

        }
    }
}
