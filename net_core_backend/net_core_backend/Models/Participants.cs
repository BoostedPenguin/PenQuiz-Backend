using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Participants
    {
        public Participants()
        {
        }

        public int Id { get; set; }
        public string AvatarName { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public bool IsBot { get; set; }
        public int Score { get; set; }

        public virtual GameInstance Game { get; set; }
        public virtual Users Player { get; set; }
    }
}
