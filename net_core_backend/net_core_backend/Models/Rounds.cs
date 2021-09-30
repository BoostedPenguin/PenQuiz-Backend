using System;
using System.Collections.Generic;

namespace net_core_backend.Models
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
            RoundQuestion = new HashSet<RoundQuestion>();
        }

        public int Id { get; set; }
        
        /// <summary>
        /// Should be a fixed string either: PVP || Neutral
        /// For the attack mode
        /// </summary>
        public RoundStage RoundStage { get; set; }
        public int AttackerId { get; set; }
        public int? DefenderId { get; set; }
        public int GameInstanceId { get; set; }
        public string Description { get; set; }
        public int? RoundWinnerId { get; set; }

        public virtual GameInstance GameInstance { get; set; }
        public virtual ICollection<RoundQuestion> RoundQuestion { get; set; }
    }
}
