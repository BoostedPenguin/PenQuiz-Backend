using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Models.Requests
{
    public class CreateMultipleChoiceQuestionRequest
    {
        [Required]
        public string Question { get; set; }
        [Required]
        public string Answer { get; set; }
        [Required]
        public string[] WrongAnswers { get; set; } = new string[3];
    }
}
