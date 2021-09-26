using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Participants
    {
        public Participants()
        {
            GameInstance = new HashSet<GameInstance>();
        }

        public int Id { get; set; }
        public int? PlayerId { get; set; }
        public int? GameId { get; set; }
        public int? Score { get; set; }

        public virtual GameInstance Game { get; set; }
        public virtual Users Player { get; set; }
        public virtual ICollection<GameInstance> GameInstance { get; set; }
    }
}
