using Microsoft.EntityFrameworkCore;
using QuestionService.Context;
using QuestionService.Data;
using QuestionService.Data.Models;
using QuestionService.Data.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Services
{
    public interface INumberQuestionsService
    {
        Task AddNumberQuestion(CreateNumberQuestionRequest request, string userName, string role);
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



        public async Task AddNumberQuestion(CreateNumberQuestionRequest request, string username, string role)
        {
            var isAnswerNumber = long.TryParse(request.Answer, out _);
            
            if (!isAnswerNumber)
                throw new ArgumentException("The provided answer isn't a number");

            using var db = contextFactory.CreateDbContext();

            // Uppercase first char
            request.Question = Extensions.FirstCharToUpper(request.Question);

            // Add question mark in the end if missing
            if (!request.Question.EndsWith("?"))
                request.Question = request.Question += "?";

            var existing = await db.Questions
                .FirstOrDefaultAsync(x => x.Question.ToLower() == request.Question.ToLower() && x.Type == "number");

            if (existing != null)
                throw new ArgumentException("This question already exists in our db.");

            switch (role)
            {
                case "user":
                    await db.AddAsync(new Questions(request.Question, request.Answer, username));
                    break;

                case "admin":
                    await db.AddAsync(new Questions(request.Question, request.Answer, username, true));
                    break;
                default:
                    throw new ArgumentException("The users role isn't recognized by the system");
            }

            await db.SaveChangesAsync();
        }

        public async Task<List<Questions>> GetNumberQuestions(List<int> amountRoundId, string sessionId, int gameInstanceId)
        {
            if (amountRoundId.Count() == 0)
                return new List<Questions>();
             
            using var db = contextFactory.CreateDbContext();

            var questions = await db.Questions
                .Include(x => x.Answers)
                .Include(x => x.GameSessionQuestions)
                .ThenInclude(x => x.GameInstance)
                .Where(x => x.Type == "number" && x.VerificationStatus == VerificationStatus.VERIFIED && !x.GameSessionQuestions
                    .Any(y => y.GameInstance.OpentDbSessionToken == sessionId))
                .ToListAsync();

            // If questions have been exchausted for this game session, repeat previous ones
            if (questions.Count() < amountRoundId.Count())
            {
                questions = await db.Questions
                    .Include(x => x.Answers)
                    .Where(x => x.Type == "number" && x.VerificationStatus == VerificationStatus.VERIFIED)
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
