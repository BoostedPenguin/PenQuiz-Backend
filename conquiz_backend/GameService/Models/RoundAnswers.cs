using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Models
{
    public partial class RoundAnswers
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AnswerId { get; set; }
        public DateTime AnsweredAt { get; set; }
        public int RoundId { get; set; }

        public virtual Answers Answer { get; set; }
        public virtual Rounds Round { get; set; }
    }
}
