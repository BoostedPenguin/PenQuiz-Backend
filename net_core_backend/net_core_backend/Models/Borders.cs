using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Borders
    {
        public int ThisTerritory { get; set; }
        public int NextToTerritory { get; set; }

        public virtual MapTerritory ThisTerritoryReference { get; set; }
        public virtual MapTerritory NextToTerritoryReference { get; set; }
    }
}
