using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class RoundsHistory
    {
        public int Id { get; set; }
        public int? RoundId { get; set; }
        public int? GameInstanceId { get; set; }
        public string Description { get; set; }
        public int? AttackerId { get; set; }
        public int? DefenderId { get; set; }
        public int? RoundWinnerId { get; set; }
    }
}
