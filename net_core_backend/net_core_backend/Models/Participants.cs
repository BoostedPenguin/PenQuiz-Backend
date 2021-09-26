using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Participants
    {
        public int Id { get; set; }
        public int? PlayerId { get; set; }
        public int? GameId { get; set; }
        public int? Score { get; set; }
    }
}
