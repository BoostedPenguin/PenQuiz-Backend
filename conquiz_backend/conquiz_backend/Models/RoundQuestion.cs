using System;
using System.Collections.Generic;

namespace conquiz_backend.Models
{
    public partial class RoundQuestion
    {
        public int Id { get; set; }
        public int RoundId { get; set; }
        public int QuestionId { get; set; }

        public virtual Questions Question { get; set; }
        public virtual Rounds Round { get; set; }
    }
}
