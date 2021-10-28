using System;
using System.Collections.Generic;

namespace GameService.Models
{
    public partial class Users
    {
        public Users()
        {
            Participants = new HashSet<Participants>();
            RefreshToken = new HashSet<RefreshToken>();
        }

        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public bool IsInGame { get; set; }

        public virtual ICollection<Participants> Participants { get; set; }
        public virtual ICollection<RefreshToken> RefreshToken { get; set; }
    }
}
