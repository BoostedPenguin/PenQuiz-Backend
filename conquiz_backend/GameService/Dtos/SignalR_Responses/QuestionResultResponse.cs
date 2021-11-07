using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos.SignalR_Responses
{
    public class QuestionResultResponse
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Type { get; set; }
        public int WinnerId { get; set; }
        public List<AnswerClientResponse> Answers { get; set; }
    }

    public class AnswerResultResponse
    {
        public int Id { get; set; }
        public string Answer { get; set; }
        public bool Correct { get; set; }
    }
}
