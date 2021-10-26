using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Models
{
    public partial class Users
    {
        public Users()
        {
            RefreshToken = new HashSet<RefreshToken>();
        }

        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public bool IsBanned { get; set; }
        public bool IsOnline { get; set; }
        public bool Provider { get; set; }
        public bool IsInGame { get; set; }

        public virtual ICollection<RefreshToken> RefreshToken { get; set; }
    }
}
