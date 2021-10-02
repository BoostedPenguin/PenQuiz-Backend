using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public enum GameState
    {
        IN_LOBBY,
        IN_PROGRESS,
        FINISHED,
        CANCELED
    }
    public partial class GameInstance
    {
        public GameInstance()
        {
            ObjectTerritory = new HashSet<ObjectTerritory>();
            Participants = new HashSet<Participants>();
            Rounds = new HashSet<Rounds>();
        }

        public int Id { get; set; }
        public int ResultId { get; set; }
        public int QuestionTimerSeconds { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Mapid { get; set; }
        public int ParticipantsId { get; set; }
        public int GameCreatorId { get; set; }
        public GameState GameState { get; set; }
        public string InvitationLink { get; set; }

        public virtual Maps Map { get; set; }
        public virtual GameResult Result { get; set; }
        public virtual ICollection<ObjectTerritory> ObjectTerritory { get; set; }
        public virtual ICollection<Participants> Participants { get; set; }
        public virtual ICollection<Rounds> Rounds { get; set; }
    }
}
