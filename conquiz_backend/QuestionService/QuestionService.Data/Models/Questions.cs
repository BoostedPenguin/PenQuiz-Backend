using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestionService.Data.Models
{
    public partial class Questions
    {
        public Questions()
        {
            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();
        }

        // For number questions
        public Questions(string question, string answer)
        {
            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();

            Type = "number";
            Question = question;
            Answers.Add(new Answers()
            {
                Answer = answer,
                Correct = true,
            });
        }

        public int Id { get; set; }
        public string Question { get; set; }
        public string Type { get; set; }
        public string Difficulty { get; set; }
        public string Category { get; set; }

        [NotMapped]
        public int RoundId { get; set; }
        public virtual ICollection<GameSessionQuestions> GameSessionQuestions { get; set; }
        public virtual ICollection<Answers> Answers { get; set; }
    }
}
