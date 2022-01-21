using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestionService.Data.Models.Requests
{
    public class VerifyQuestionRequest
    {
        [Required]
        public int QuestionId { get; set; }
    }
}
