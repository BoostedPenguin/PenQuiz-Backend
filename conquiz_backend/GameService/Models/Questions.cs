using System;
using System.Collections.Generic;

namespace GameService.Models
{
    public partial class Questions
    {
        public Questions()
        {
            Answers = new HashSet<Answers>();
        }

        public int Id { get; set; }
        public string Question { get; set; }
        public int RoundsId { get; set; }
        public string Type { get; set; }
        public int RoundId { get; set; }

        public virtual ICollection<Answers> Answers { get; set; }
        public virtual Round Round { get; set; }
        public virtual PvpRound PvpRoundNum { get; set; }
    }
}
