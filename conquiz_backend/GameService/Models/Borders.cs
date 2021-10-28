using System;
using System.Collections.Generic;

namespace GameService.Models
{
    public partial class Borders
    {
        public int ThisTerritory { get; set; }
        public int NextToTerritory { get; set; }

        public virtual MapTerritory NextToTerritoryNavigation { get; set; }
        public virtual MapTerritory ThisTerritoryNavigation { get; set; }
    }
}
