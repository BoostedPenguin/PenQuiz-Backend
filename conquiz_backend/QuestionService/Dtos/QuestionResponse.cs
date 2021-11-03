using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Dtos
{
    public class QuestionResponse
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Type { get; set; }
        public string Event { get; set; }

        public virtual List<AnswerResponse> Answers { get; set; } = new List<AnswerResponse>();
    }
}
