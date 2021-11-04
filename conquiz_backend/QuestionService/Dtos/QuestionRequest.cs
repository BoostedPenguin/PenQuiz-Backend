﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Dtos
{
    public class QuestionRequest
    {
        public int GameInstanceId { get; set; }

        public List<int> MultipleChoiceQuestionsRoundId { get; set; } = new List<int>();

        // Total Count of number questions .count()
        // Each value represents a RoundId
        public List<int> NumberQuestionsRoundId { get; set; } = new List<int>();
        public string Event { get; set; }
    }
}
