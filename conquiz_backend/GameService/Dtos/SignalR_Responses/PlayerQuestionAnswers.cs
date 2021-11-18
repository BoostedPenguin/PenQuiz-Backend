using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos.SignalR_Responses
{
    public class PlayerQuestionAnswers
    {
        public int CorrectAnswerId { get; set; }
        public List<PlayerIdAnswerId> PlayerAnswers { get; set; }
    }

    public class PlayerIdAnswerId
    {
        public int Id { get; set; }
        public int AnswerId { get; set; }
    }
}
