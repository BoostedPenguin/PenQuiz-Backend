using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class MapTerritory
    {
        public MapTerritory()
        {
            BordersBordersTerNavigation = new HashSet<Borders>();
            BordersThisTerNavigation = new HashSet<Borders>();
        }

        public int Id { get; set; }
        public string TerritoryName { get; set; }
        public int MapId { get; set; }

        public virtual Maps Map { get; set; }
        public virtual ICollection<Borders> BordersBordersTerNavigation { get; set; }
        public virtual ICollection<Borders> BordersThisTerNavigation { get; set; }
    }
}
