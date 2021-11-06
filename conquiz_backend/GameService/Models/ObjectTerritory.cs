using System;
using System.Collections.Generic;

namespace GameService.Models
{
    public partial class ObjectTerritory
    {
        public int Id { get; set; }
        public int MapTerritoryId { get; set; }
        public int GameInstanceId { get; set; }
        public bool IsCapital { get; set; }
        public int TerritoryScore { get; set; }
        public int? TakenBy { get; set; }

        public ICollection<Rounds> Rounds { get; set; }
        public virtual GameInstance GameInstance { get; set; }
        public virtual MapTerritory MapTerritory { get; set; }
    }
}
