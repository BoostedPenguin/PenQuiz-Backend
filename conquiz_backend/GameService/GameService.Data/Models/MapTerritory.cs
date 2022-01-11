using System;
using System.Collections.Generic;

namespace GameService.Data.Models
{
    public partial class MapTerritory
    {
        public MapTerritory()
        {
            BordersNextToTerritoryNavigation = new HashSet<Borders>();
            BordersThisTerritoryNavigation = new HashSet<Borders>();
            ObjectTerritory = new HashSet<ObjectTerritory>();
        }

        public int Id { get; set; }
        public string TerritoryName { get; set; }
        public int MapId { get; set; }

        public virtual Maps Map { get; set; }
        public virtual ICollection<Borders> BordersNextToTerritoryNavigation { get; set; }
        public virtual ICollection<Borders> BordersThisTerritoryNavigation { get; set; }
        public virtual ICollection<ObjectTerritory> ObjectTerritory { get; set; }
    }
}
