using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class GameInstance
    {
        public int Id { get; set; }
        public int? ResultId { get; set; }
        public int? QuestionTimerSeconds { get; set; }
        public bool? InProgress { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
