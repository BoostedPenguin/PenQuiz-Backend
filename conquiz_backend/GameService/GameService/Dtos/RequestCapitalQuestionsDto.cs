using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos
{
    public class RequestCapitalQuestionsDto
    {
        public string GameGlobalIdentifier { get; set; }
        public List<int> QuestionsCapitalRoundId { get; set; } = new List<int>();
        public string Event { get; set; }
    }
}
