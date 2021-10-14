using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class ObjectTerritory
    {
        public int Id { get; set; }
        public int MapTerritoryId { get; set; }
        public int GameInstanceId { get; set; }
        public int? TakenBy { get; set; }
        public int? AttackedBy { get; set; }

        public virtual GameInstance GameInstance { get; set; }
        public virtual MapTerritory MapTerritory { get; set; }
    }
}
