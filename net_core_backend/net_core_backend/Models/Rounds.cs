using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Rounds
    {
        public Rounds()
        {
            RoundsHistory = new HashSet<RoundsHistory>();
        }

        public int Id { get; set; }
        public int? TotalRounds { get; set; }

        public virtual ICollection<RoundsHistory> RoundsHistory { get; set; }
    }
}
