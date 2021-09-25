using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Borders
    {
        public int ThisTer { get; set; }
        public int BordersTer { get; set; }

        public virtual MapTerritory BordersTerNavigation { get; set; }
        public virtual MapTerritory ThisTerNavigation { get; set; }
    }
}
