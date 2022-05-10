using GameService.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos.SignalR_Responses
{
    public class QuestionClientResponse
    {
        public bool IsLastQuestion { get; set; } = false;
        public bool IsNeutral { get; set; }
        
        // NULL if round isn't pvp and the attacked territory isn't capital
        // Rounds remanining to take this territory (plus this current one)
        // ex. 2 rounds in total remaining to take the territory -> value = 2
        public int? CapitalRoundsRemaining { get; set; }
        public int Id { get; set; }
        public string Question { get; set; }
        public string Type { get; set; }
        public List<AnswerClientResponse> Answers { get; set; }


        public ParticipantsResponse[] Participants { get; set; }
        public int AttackerId { get; set; }
        public int DefenderId { get; set; }
    }

    public class AnswerClientResponse
    {
        public int Id { get; set; }
        public string Answer { get; set; }
    }
}
