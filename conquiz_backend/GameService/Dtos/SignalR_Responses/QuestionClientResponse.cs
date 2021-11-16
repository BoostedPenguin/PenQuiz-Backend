using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos.SignalR_Responses
{
    public class QuestionClientResponse
    {
        public bool IsNeutral { get; set; }
        public int Id { get; set; }
        public string Question { get; set; }
        public string Type { get; set; }
        public List<AnswerClientResponse> Answers { get; set; }


        public Participants[] Participants { get; set; }
        public int AttackerId { get; set; }
        public int DefenderId { get; set; }
    }

    public class AnswerClientResponse
    {
        public int Id { get; set; }
        public string Answer { get; set; }
    }
}
