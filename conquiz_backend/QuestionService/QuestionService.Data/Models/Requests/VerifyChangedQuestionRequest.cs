using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestionService.Data.Models.Requests
{
    public class VerifyChangedQuestionRequest
    {
        [Required]
        public int QuestionId { get; set; }
        [Required]
        public string Question { get; set; }
        [Required]
        public string Answer { get; set; }
        
        
        public List<string> WrongAnswers { get; set; }
    }
}
