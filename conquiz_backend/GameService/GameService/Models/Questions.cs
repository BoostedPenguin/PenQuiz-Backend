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
        public int? RoundId { get; set; }
        public int? PvpRoundId { get; set; }
        public int? CapitalRoundMCId { get; set; }
        public int? CapitalRoundNumberId { get; set; }
        public string Type { get; set; }

        public virtual ICollection<Answers> Answers { get; set; }
        public virtual CapitalRound CapitalRoundNumber { get; set; }
        public virtual CapitalRound CapitalRoundMultiple { get; set; }
        public virtual Round Round { get; set; }
        public virtual PvpRound PvpRoundNum { get; set; }
    }
}
