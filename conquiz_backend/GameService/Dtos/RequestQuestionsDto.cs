using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos
{
    public class RequestQuestionsDto
    {
        public int GameInstanceId { get; set; }
        public string Event { get; set; }
    }
}
