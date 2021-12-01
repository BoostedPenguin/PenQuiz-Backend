using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GameService.Dtos.SignalR_Responses
{
    public class NumberPlayerQuestionAnswers
    {
        public string CorrectAnswer { get; set; }
        public List<NumberPlayerIdAnswer> PlayerAnswers { get; set; }
    }

    public class NumberPlayerIdAnswer
    {
        public int PlayerId { get; set; }
        public string Answer { get; set; }
        [JsonIgnore]
        public long DifferenceWithCorrectNumber { get; set; }
        public string DifferenceWithCorrect { get; set; }
        [JsonIgnore]
        public double TimeElapsedNumber { get; set; }
        public string TimeElapsed { get; set; }
        public bool Winner { get; set; }
    }
}
