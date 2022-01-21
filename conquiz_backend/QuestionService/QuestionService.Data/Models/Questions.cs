using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace QuestionService.Data.Models
{
    public partial class Questions
    {
        private readonly Random rng = new Random();

        public Questions()
        {
            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();
        }



        /// <summary>
        /// Add number questions system submitted
        /// </summary>
        /// <param name="question"></param>
        /// <param name="answer"></param>
        public Questions(string question, string answer)
        {
            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();

            Type = "number";
            
            SubmittedAt = DateTime.Now;
            IsVerified = true;
            VerifiedAt = DateTime.Now;

            Question = question;
            Answers.Add(new Answers()
            {
                Answer = answer,
                Correct = true,
            });
        }


        /// <summary>
        /// Add number questions by user
        /// </summary>
        /// <param name="question"></param>
        /// <param name="answer"></param>
        public Questions(string question, string answer, string userName, bool isAdmin = false)
        {
            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();

            Type = "number";
            SubmittedAt = DateTime.Now;
            SubmittedByUsername = userName;

            Question = question;
            Answers.Add(new Answers()
            {
                Answer = answer,
                Correct = true,
            });

            if (isAdmin)
            {
                IsVerified = true;

                VerifiedAt = DateTime.Now;
            }
            else
            {
                IsVerified = false;
            }
        }

        /// <summary>
        /// Add multiple choice question by user
        /// </summary>
        /// <param name="question"></param>
        /// <param name="correctAnswer"></param>
        /// <param name="wrongAnswers"></param>
        /// <param name="userName"></param>
        /// <param name="isAdmin"></param>
        /// <exception cref="ArgumentException"></exception>
        public Questions(string question, string correctAnswer, string[] wrongAnswers, string userName, bool isAdmin = false)
        {
            if (wrongAnswers.Length != 3)
                throw new ArgumentException("Wrong answers need to be exactly 3");

            Answers = new HashSet<Answers>();
            GameSessionQuestions = new HashSet<GameSessionQuestions>();

            SubmittedAt = DateTime.Now;
            SubmittedByUsername = userName;
            Type = "multiple";
            Question = question;

            // Verified status
            if (isAdmin)
            {
                IsVerified = true;

                VerifiedAt = DateTime.Now;
            }
            else
            {
                IsVerified = false;
            }

            // Add answers
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

        
        /// <summary>
        /// Add multiple choice question by system
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
        public string SubmittedByUsername { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public bool? IsVerified { get; set; }

        [NotMapped]
        public int RoundId { get; set; }
        public virtual ICollection<GameSessionQuestions> GameSessionQuestions { get; set; }
        public virtual ICollection<Answers> Answers { get; set; }
    }
}
