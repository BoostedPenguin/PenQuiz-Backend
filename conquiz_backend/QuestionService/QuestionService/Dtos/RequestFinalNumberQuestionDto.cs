using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Dtos
{
    public class RequestFinalNumberQuestionDto
    {
        public string GameGlobalIdentifier { get; set; }
        public int QuestionFinalRoundId { get; set; }
        public string Event { get; set; }
    }
}
