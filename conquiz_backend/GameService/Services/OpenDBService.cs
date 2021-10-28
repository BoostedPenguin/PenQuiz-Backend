using Microsoft.AspNetCore.Http;
using GameService.Context;
using GameService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GameService.Services
{
    public interface IOpenDBService
    {
        Task<List<Questions>> GetMultipleChoiceQuestion(int gameInstanceId);
    }

    public class OpenDBService : DataService<DefaultModel>, IOpenDBService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IHttpClientFactory clientFactory;

        /// <summary>
        /// Game instance id | sessionToken
        /// </summary>
        Dictionary<int, string> sessionTokens = new Dictionary<int, string>();

        public OpenDBService(IDbContextFactory<DefaultContext> _contextFactory, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base(_contextFactory)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.clientFactory = clientFactory;
        }

        class QuestionResponse
        {
            public int Response_Code { get; set; }

            public List<OpenTDBQuestion> Results { get; set; } = new List<OpenTDBQuestion>();
        }
        class OpenTDBQuestion
        {
            public string Category { get; set; }
            public string Type { get; set; }
            public string Difficulty { get; set; }
            public string Question { get; set; }
            public string Correct_Answer { get; set; }
            public List<string> Incorrect_Answers { get; set; } = new List<string>();
        }

        class SessionTokenResponse
        {
            public int Response_Code { get; set; }
            public string Response_Message { get; set; }
            public string Token { get; set; }
        }

        public async Task<List<Questions>> GetMultipleChoiceQuestion(int gameInstanceId)
        {
            var client = clientFactory.CreateClient();

            var sessionToken = await GenerateSessionToken(client, gameInstanceId);

            using var response = await client.GetAsync($"https://opentdb.com/api.php?amount=1&type=multiple&token={sessionToken}");

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync();

            var convertedResponse = JsonConvert.DeserializeObject<QuestionResponse>(str);


            var questions = new List<Questions>();
            foreach(var a in convertedResponse.Results)
            {
                var que = new Questions
                {
                    Question = a.Question,
                    IsNumberQuestion = false
                };

                // Add the correct answer
                que.Answers.Add(new Answers()
                {
                    Answer = a.Correct_Answer,
                    Correct = true,
                });

                // Add the wrong answers
                foreach(var incAns in a.Incorrect_Answers)
                {
                    que.Answers.Add(new Answers()
                    {
                        Answer = incAns,
                        Correct = false,
                    });
                }

                questions.Add(que);
            }

            return questions;
        }

        public async Task<string> GenerateSessionToken(HttpClient client, int gameInstanceId)
        {
            // If there is an existing token return it without making a new one
            if (sessionTokens.ContainsKey(gameInstanceId))
            {
                return sessionTokens[gameInstanceId];
            }

            using var response =
                await client.GetAsync("https://opentdb.com/api_token.php?command=request");

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync();

            var convertedResponse = JsonConvert.DeserializeObject<SessionTokenResponse>(str);

            sessionTokens.Add(gameInstanceId, convertedResponse.Token);

            return convertedResponse.Token;
        }
    }
}
