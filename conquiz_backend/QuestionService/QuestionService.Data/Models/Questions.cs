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

        // For MC questions
        public Questions(string question, string correctAnswer, string[] wrongAnswers, string category = null, string difficulty = null)
        {
            if (wrongAnswers.Length != 3)
                throw new ArgumentException("Wrong answers need to be exactly 3");

            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();

            Type = "multiple";
            Question = question;
            Difficulty = difficulty;
            Category = category;

            Answers.Add(new Answers()
            {
                Answer = correctAnswer,
                Correct = true,
            });

            foreach(var wAnswer in wrongAnswers)
            {
                Answers.Add(new Answers()
                {
                    Answer = wAnswer,
                    Correct = false,
                });
            }
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
