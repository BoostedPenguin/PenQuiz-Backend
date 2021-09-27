using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class RoundsHistory
    {
        public RoundsHistory()
        {
            RoundQuestion = new HashSet<RoundQuestion>();
        }

        public int Id { get; set; }
        public int RoundId { get; set; }
        public int GameInstanceId { get; set; }
        public string Description { get; set; }
        public int AttackerId { get; set; }
        public int DefenderId { get; set; }
        public int RoundWinnerId { get; set; }

        public virtual GameInstance GameInstance { get; set; }
        public virtual Rounds Round { get; set; }
        public virtual ICollection<RoundQuestion> RoundQuestion { get; set; }
    }
}
