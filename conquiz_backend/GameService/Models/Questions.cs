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
        public bool PrimaryQuestion { get; set; }
        public int RoundsId { get; set; }
        public string Type { get; set; }

        public virtual ICollection<Answers> Answers { get; set; }
        public virtual Rounds Rounds { get; set; }
    }
}
