using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Questions
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public bool? IsNumberQuestion { get; set; }
    }
}
