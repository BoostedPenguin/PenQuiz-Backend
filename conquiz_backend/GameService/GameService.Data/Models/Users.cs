using System;
using System.Collections.Generic;

namespace GameService.Data.Models
{
    public partial class Users
    {
        public Users()
        {
            Participants = new HashSet<Participants>();
            OwnedCharacters = new HashSet<Character>();
        }

        public int Id { get; set; }
        public string Username { get; set; }
        public bool IsInGame { get; set; }
        public bool IsBot { get; set; }
        public string UserGlobalIdentifier { get; set; }

        public virtual ICollection<Character> OwnedCharacters { get; set; }
        public virtual ICollection<Participants> Participants { get; set; }
    }
}
