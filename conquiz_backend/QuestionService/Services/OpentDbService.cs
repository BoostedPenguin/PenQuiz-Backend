using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuestionService.Context;
using QuestionService.Models;
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

    public interface IOpenDBService
    {
        Task<List<Questions>> GetMultipleChoiceQuestion(int gameInstanceId);
    }

    public class OpenDBService : IOpenDBService
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly IMapper mapper;

        /// <summary>
        /// Game instance id | sessionToken
        /// </summary>
        Dictionary<int, string> sessionTokens = new Dictionary<int, string>();

        public OpenDBService(IDbContextFactory<DefaultContext> _contextFactory, IHttpClientFactory clientFactory, IMapper mapper)
        {
            this.clientFactory = clientFactory;
            this.mapper = mapper;
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

            var convertedResponse = JsonConvert.DeserializeObject<OpenTDBResponse>(str);


            var questions = new List<Questions>();
            foreach (var a in convertedResponse.Results)
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
