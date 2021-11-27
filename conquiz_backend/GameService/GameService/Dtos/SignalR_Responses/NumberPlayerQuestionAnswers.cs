using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos.SignalR_Responses
{
    public class NumberPlayerQuestionAnswers
    {
        public int CorrectAnswer { get; set; }
        public List<NumberPlayerIdAnswer> PlayerAnswers { get; set; }
    }

    public class NumberPlayerIdAnswer
    {
        public int PlayerId { get; set; }
        public int? Answer { get; set; }
        public int DifferenceWithCorrect { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public bool Winner { get; set; }
    }
}
