using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Questions
    {
        public Questions()
        {
            Answers = new HashSet<Answers>();
            RoundQuestion = new HashSet<RoundQuestion>();
        }

        public int Id { get; set; }
        public string Question { get; set; }
        public bool? IsNumberQuestion { get; set; }

        public virtual ICollection<Answers> Answers { get; set; }
        public virtual ICollection<RoundQuestion> RoundQuestion { get; set; }
    }
}
