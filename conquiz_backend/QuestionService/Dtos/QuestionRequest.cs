using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Dtos
{
    public class QuestionRequest
    {
        public int GameInstanceId { get; set; }
        public int NumberQuestionsAmount { get; set; }
        public int MultipleChoiceQuestionsAmount { get; set; }
    }
}
