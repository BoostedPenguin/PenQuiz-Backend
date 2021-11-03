using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Models.Requests
{
    public class CreateNumberQuestionRequest
    {
        [Required]
        public string Question { get; set; }
        [Required]
        public int Answer { get; set; }
    }
}
