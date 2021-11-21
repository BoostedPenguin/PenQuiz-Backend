using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos
{
    public class QResponse
    {
        public int GameInstanceId { get; set; }
        public string Event { get; set; }
        public virtual QuestionResponse[] QuestionResponses { get; set; }

    }

    public class QuestionResponse
    {
        public int Id { get; set; }
        public int RoundId { get; set; }
        public string Question { get; set; }
        public string Type { get; set; }
        public virtual List<AnswerResponse> Answers { get; set; } = new List<AnswerResponse>();
    }

    public class AnswerResponse
    {
        public int Id { get; set; }
        public string Answer { get; set; }
        public bool Correct { get; set; }
    }
}
