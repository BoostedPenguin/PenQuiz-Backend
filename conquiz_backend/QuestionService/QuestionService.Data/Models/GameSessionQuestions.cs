using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Data.Models
{
    public partial class GameSessionQuestions
    {
        public int Id { get; set; }
        public int GameInstanceId { get; set; }
        public int QuestionId { get; set; }

        public virtual Questions Question { get; set; }
        public virtual GameInstance GameInstance { get; set; }
    }
}
