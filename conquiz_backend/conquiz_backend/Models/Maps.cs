using System;
using System.Collections.Generic;

namespace conquiz_backend.Models
{
    public partial class Maps
    {
        public Maps()
        {
            GameInstance = new HashSet<GameInstance>();
            MapTerritory = new HashSet<MapTerritory>();
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<GameInstance> GameInstance { get; set; }
        public virtual ICollection<MapTerritory> MapTerritory { get; set; }
    }
}
