using System;
using System.Collections.Generic;

namespace QuestionService.Models
{
    public partial class Questions
    {
        public Questions()
        {
            Answers = new HashSet<Answers>();
        }

        public int Id { get; set; }
        public string Question { get; set; }
        public string Type { get; set; }
        public string Difficulty { get; set; }
        public string Category { get; set; }

        public virtual ICollection<Answers> Answers { get; set; }
    }
}
