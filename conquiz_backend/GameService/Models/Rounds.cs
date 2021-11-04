using System;
using System.Collections.Generic;

namespace GameService.Models
{
    public enum RoundStage
    {
        NOT_STARTED,
        CURRENT,
        FINISHED,
    }
    public partial class Rounds
    {
        public Rounds()
        {
        }

        public int Id { get; set; }
        
        /// <summary>
        /// Should be a fixed string either: PVP || Neutral
        /// For the attack mode
        /// </summary>
        public RoundStage RoundStage { get; set; }
        public bool IsLastUntakenTerritories { get; set; }

        // For concurrency
        // Every round you add you give +1 number to this
        // In gameInstance you have "CurrentRound" property
        // It links to the currently playing round
        // When a round is complete go to the next round by adding 1 to this round number
        // If it returns 0 items have this gameroundnumber then this is the limit
        public int GameRoundNumber { get; set; }
        public int? AttackerId { get; set; }
        public int? DefenderId { get; set; }
        public int GameInstanceId { get; set; }
        public string Description { get; set; }
        public int? RoundWinnerId { get; set; }

        public virtual Questions Question { get; set; }
        public virtual GameInstance GameInstance { get; set; }
    }
}
