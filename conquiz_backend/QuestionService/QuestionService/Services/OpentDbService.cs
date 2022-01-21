using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuestionService.Context;
using QuestionService.Data;
using QuestionService.Data.Models;
using QuestionService.Data.Models.Requests;
using QuestionService.Dtos;
using QuestionService.MessageBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuestionService.Services
{
    class OpenTDBQuestion
    {
        public string Category { get; set; }
        public string Type { get; set; }
        public string Difficulty { get; set; }
        public string Question { get; set; }
        public string Correct_Answer { get; set; }
        public List<string> Incorrect_Answers { get; set; } = new List<string>();
    }

    class OpenTDBResponse
    {
        public int Response_Code { get; set; }

        public List<OpenTDBQuestion> Results { get; set; } = new List<OpenTDBQuestion>();
    }

    public class SessionTokenRequest
    {
        public string Token { get; set; }
        public int InternalGameInstanceId { get; set; }
    }

    public interface IMCQuestionsService
    {
        Task<SessionTokenRequest> GenerateSessionToken(string gameGlobalIdentifier);
        Task<List<Questions>> GetMultipleChoiceQuestion(string sessionToken, List<int> multipleChoiceQuestions);
        Task CreateMultipleChoiceQuestion(CreateMultipleChoiceQuestionRequest request, string username, string role);
    }

    public class OpenDBService : IMCQuestionsService
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly IMapper mapper;
        private readonly IMessageBusClient messageBus;
        private readonly INumberQuestionsService numberQuestionsService;
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        const int MaxOpenDbQuestions = 10;


        public OpenDBService(IHttpClientFactory clientFactory, IMapper mapper, IMessageBusClient messageBus, INumberQuestionsService numberQuestionsService, IDbContextFactory<DefaultContext> contextFactory)
        {

            this.clientFactory = clientFactory;
            this.mapper = mapper;
            this.messageBus = messageBus;
            this.numberQuestionsService = numberQuestionsService;
            this.contextFactory = contextFactory;
        }



        class SessionTokenResponse
        {
            public int Response_Code { get; set; }
            public string Response_Message { get; set; }
            public string Token { get; set; }
        }

        public async Task<List<Questions>> GetMultipleChoiceQuestion(string sessionToken, List<int> multipleChoiceQuestions)
        {
            var client = clientFactory.CreateClient();

            var questions = new List<Questions>();

            var mcQuestionCount = multipleChoiceQuestions.Count();
            var currentRoundIndex = 0;

            while (mcQuestionCount > 0)
            {
                int currentIteration;
                if (mcQuestionCount > MaxOpenDbQuestions)
                {
                    currentIteration = MaxOpenDbQuestions;
                    mcQuestionCount -= MaxOpenDbQuestions;
                }
                else
                {
                    currentIteration = mcQuestionCount;
                    mcQuestionCount = 0;
                }

                using var response = await client.GetAsync($"https://opentdb.com/api.php?amount={currentIteration}&type=multiple&token={sessionToken}&encode=url3986");

                response.EnsureSuccessStatusCode();

                var str = await response.Content.ReadAsStringAsync();

                var convertedResponse = JsonConvert.DeserializeObject<OpenTDBResponse>(str);

                foreach (var a in convertedResponse.Results)
                {
                    var que = new Questions
                    {
                        Question = a.Question,
                        Type = "multiple",
                        Category = a.Category,
                        Difficulty = a.Difficulty,
                        RoundId = multipleChoiceQuestions[currentRoundIndex++]
                    };

                    // Add the correct answer
                    que.Answers.Add(new Answers()
                    {
                        Answer = a.Correct_Answer,
                        Correct = true,
                    });

                    // Add the wrong answers
                    foreach (var incAns in a.Incorrect_Answers)
                    {
                        que.Answers.Add(new Answers()
                        {
                            Answer = incAns,
                            Correct = false,
                        });
                    }

                    questions.Add(que);
                }
            }

            return questions;
        }


        public async Task<SessionTokenRequest> GenerateSessionToken(string gameGlobalIdentifier)
        {
            // If there is an existing token return it without making a new one

            using var db = contextFactory.CreateDbContext();

            var client = clientFactory.CreateClient();

            var gm = await db.GameInstances.FirstOrDefaultAsync(x => x.ExternalGlobalId == gameGlobalIdentifier);
            
            // Hasn't been added to db yet
            if(gm == null)
            {
                using var response =
                    await client.GetAsync("https://opentdb.com/api_token.php?command=request");

                response.EnsureSuccessStatusCode();

                var str = await response.Content.ReadAsStringAsync();

                var convertedResponse = JsonConvert.DeserializeObject<SessionTokenResponse>(str);

                gm = new GameInstance()
                {
                    ExternalGlobalId = gameGlobalIdentifier,
                    OpentDbSessionToken = convertedResponse.Token,
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

        public Task CreateMultipleChoiceQuestion(CreateMultipleChoiceQuestionRequest request, string username, string role)
        {
            throw new ArgumentException("The system uses legacy questions provider. Unable to manually add new questions");
        }
    }
}
