using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos
{
    public class RequestQuestionsDto
    {
        public int GameInstanceId { get; set; }

        public int NumberQuestionsAmount { get; set; }
        public int MultipleChoiceQuestionsAmount { get; set; }
        public string Event { get; set; }
    }
}
