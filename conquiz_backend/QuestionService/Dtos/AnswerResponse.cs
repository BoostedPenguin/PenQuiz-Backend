using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Dtos
{
    public class AnswerResponse
    {
        public int Id { get; set; }
        public string Answer { get; set; }
        public bool Correct { get; set; }
    }
}
