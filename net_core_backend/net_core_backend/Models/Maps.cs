using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Maps
    {
        public Maps()
        {
            MapTerritory = new HashSet<MapTerritory>();
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<MapTerritory> MapTerritory { get; set; }
    }
}
