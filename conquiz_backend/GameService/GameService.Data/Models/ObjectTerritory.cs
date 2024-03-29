﻿using System;
using System.Collections.Generic;

namespace GameService.Data.Models
{
    public partial class ObjectTerritory
    {
        public ObjectTerritory()
        {
            NeutralRoundsAttacks = new HashSet<AttackingNeutralTerritory>();
            PvpRounds = new HashSet<PvpRound>();
        }
        public int Id { get; set; }
        public int MapTerritoryId { get; set; }
        public int GameInstanceId { get; set; }
        public bool IsCapital { get; set; }
        public int TerritoryScore { get; set; }
        public int? TakenBy { get; set; }
        public int? AttackedBy { get; set; }

        public ICollection<AttackingNeutralTerritory> NeutralRoundsAttacks { get; set; }
        public ICollection<PvpRound> PvpRounds { get; set; }
        public virtual GameInstance GameInstance { get; set; }
        public virtual MapTerritory MapTerritory { get; set; }
    }
}
