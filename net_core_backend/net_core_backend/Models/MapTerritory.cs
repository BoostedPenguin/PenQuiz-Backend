using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class MapTerritory
    {
        public MapTerritory()
        {
            BordersNextToTerritoryReference = new HashSet<Borders>();
            BordersThisTerritoryReference = new HashSet<Borders>();
        }

        public int Id { get; set; }
        public string TerritoryName { get; set; }
        public int MapId { get; set; }

        public virtual Maps Map { get; set; }
        public virtual ICollection<Borders> BordersNextToTerritoryReference { get; set; }
        public virtual ICollection<Borders> BordersThisTerritoryReference { get; set; }
    }
}
