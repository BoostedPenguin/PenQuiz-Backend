using Microsoft.AspNetCore.Http;
using conquiz_backend.Context;
using conquiz_backend.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace conquiz_backend.Services
{
    public class OpenDBService : DataService<DefaultModel>
    {
        private readonly IContextFactory contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IHttpClientFactory clientFactory;

        Dictionary<GameInstance, string> sessionTokens = new Dictionary<GameInstance, string>();
        
        public OpenDBService(IContextFactory _contextFactory, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.clientFactory = clientFactory;
        }

        public class QuestionResponse
        {
            public int Response_Code { get; set; }

            public List<Questions> Results { get; set; } = new List<Questions>();
        }
        public class Questions
        {
            public string Category { get; set; }
            public string Type { get; set; }
            public string Difficulty { get; set; }
            public string Question { get; set; }
            public string Correct_Answer { get; set; }
            public List<string> Incorrect_Answers { get; set; } = new List<string>();
        }

        public class SessionTokenResponse
        {
            public int Response_Code { get; set; }
            public string Response_Message { get; set; }
            public string Token { get; set; }
        }

        public async Task<List<Questions>> GetMultipleChoiceQuestion(GameInstance instance)
        {
            var client = clientFactory.CreateClient();

            var sessionToken = await GenerateSessionToken(client, instance);

            using var response = await client.GetAsync($"https://opentdb.com/api.php?amount=1&type=multiple&token={sessionToken}");

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync();

            var convertedResponse = JsonConvert.DeserializeObject<QuestionResponse>(str);

            return convertedResponse.Results;
        }

        public async Task<string> GenerateSessionToken(HttpClient client, GameInstance gameInstance)
        {
            // If there is an existing token return it without making a new one
            if(sessionTokens.ContainsKey(gameInstance))
            {
                return sessionTokens[gameInstance];
            }

            using var response = 
                await client.GetAsync("https://opentdb.com/api_token.php?command=request");

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync();

            var convertedResponse = JsonConvert.DeserializeObject<SessionTokenResponse>(str);

            sessionTokens.Add(gameInstance, convertedResponse.Token);

            return convertedResponse.Token;
        }
    }
}
