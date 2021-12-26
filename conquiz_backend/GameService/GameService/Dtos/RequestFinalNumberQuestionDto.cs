using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos
{
    public class RequestFinalNumberQuestionDto
    {
        public int GameInstanceId { get; set; }
        public int QuestionFinalRoundId { get; set; }
        public string Event { get; set; }
    }
}
