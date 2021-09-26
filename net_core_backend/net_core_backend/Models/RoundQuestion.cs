using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class RoundQuestion
    {
        public int Id { get; set; }
        public int? RoundId { get; set; }
        public int? QuestionId { get; set; }
    }
}
