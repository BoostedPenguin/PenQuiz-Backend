using GameService.Data.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameService.Dtos.SignalR_Responses
{
    public class WizardUseMultipleChoiceHint
    {
        // Contains only 2 answers
        // 1 of them is correct, other one is wrong
        public List<AnswerClientResponse> Answers { get; set; }

        [JsonIgnore]
        public string GameLink { get; set; }

    }
}
