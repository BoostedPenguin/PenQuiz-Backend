using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace QuestionService.Data.Models
{
    public partial class Questions
    {
        public Questions()
        {
            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();
        }

        /// <summary>
        /// Add number questions
        /// </summary>
        /// <param name="question"></param>
        /// <param name="answer"></param>
        public Questions(string question, string answer, bool isVerified = true)
        {
            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();

            IsVerified = isVerified;
            Type = "number";
            Question = question;
            Answers.Add(new Answers()
            {
                Answer = answer,
                Correct = true,
            });
        }
        private readonly Random rng = new Random();
        
        /// <summary>
        /// Add multiple choice question
        /// </summary>
        /// <param name="question"></param>
        /// <param name="correctAnswer"></param>
        /// <param name="wrongAnswers"></param>
        /// <param name="category"></param>
        /// <param name="difficulty"></param>
        public Questions(string question, string correctAnswer, string[] wrongAnswers, bool isVerified = true, string category = null, string difficulty = null)
        {
            if (wrongAnswers.Length != 3)
                throw new ArgumentException("Wrong answers need to be exactly 3");

            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();

            Type = "multiple";
            Question = question;
            Difficulty = difficulty;
            Category = category;
            IsVerified = isVerified;

            var answers = new List<Answers>
            {
                new Answers()
                {
                    Answer = correctAnswer,
                    Correct = true,
                }
            };

            foreach (var wAnswer in wrongAnswers)
            {
                answers.Add(new Answers()
                {
                    Answer = wAnswer,
                    Correct = false,
                });
            }

            // Shuffle list so the correct answer isn't always first
            Answers = answers.OrderBy(x => rng.Next()).ToList();
        }

        public int Id { get; set; }
        public string Question { get; set; }
        public string Type { get; set; }
        public string Difficulty { get; set; }
        public string Category { get; set; }
        public bool? IsVerified { get; set; }

        [NotMapped]
        public int RoundId { get; set; }
        public virtual ICollection<GameSessionQuestions> GameSessionQuestions { get; set; }
        public virtual ICollection<Answers> Answers { get; set; }
    }
}
