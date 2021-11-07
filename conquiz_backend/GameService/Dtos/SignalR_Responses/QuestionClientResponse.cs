using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos.SignalR_Responses
{
    public class QuestionClientResponse
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Type { get; set; }
        public List<AnswerClientResponse> Answers { get; set; }
    }

    public class AnswerClientResponse
    {
        public int Id { get; set; }
        public string Answer { get; set; }
    }
}
