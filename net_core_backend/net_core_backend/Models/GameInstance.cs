using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class GameInstance
    {
        public GameInstance()
        {
            ObjectTerritory = new HashSet<ObjectTerritory>();
            ParticipantsNavigation = new HashSet<Participants>();
            RoundsHistory = new HashSet<RoundsHistory>();
        }

        public int Id { get; set; }
        public int? ResultId { get; set; }
        public int? QuestionTimerSeconds { get; set; }
        public bool? InProgress { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Mapid { get; set; }
        public int? ParticipantsId { get; set; }

        public virtual Maps Map { get; set; }
        public virtual Participants Participants { get; set; }
        public virtual GameResult Result { get; set; }
        public virtual ICollection<ObjectTerritory> ObjectTerritory { get; set; }
        public virtual ICollection<Participants> ParticipantsNavigation { get; set; }
        public virtual ICollection<RoundsHistory> RoundsHistory { get; set; }
    }
}
