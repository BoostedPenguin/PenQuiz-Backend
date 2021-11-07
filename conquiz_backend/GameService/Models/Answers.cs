using System;
using System.Collections.Generic;

namespace GameService.Models
{
    public partial class Answers
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Answer { get; set; }
        public bool Correct { get; set; }

        public virtual Questions Question { get; set; }
    }
}
