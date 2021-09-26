using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class GameResult
    {
        public GameResult()
        {
            GameInstance = new HashSet<GameInstance>();
        }

        public int Id { get; set; }
        public string Description { get; set; }

        public virtual ICollection<GameInstance> GameInstance { get; set; }
    }
}
