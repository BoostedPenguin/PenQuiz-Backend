﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Dtos
{
    public class CapitalQuestionRequest
    {
        public string GameGlobalIdentifier { get; set; }
        public List<int> QuestionsCapitalRoundId { get; set; } = new List<int>();
        public string Event { get; set; }
    }
}
