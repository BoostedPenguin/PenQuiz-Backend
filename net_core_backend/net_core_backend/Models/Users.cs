using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Users
    {
        public Users()
        {
            RefreshToken = new HashSet<RefreshToken>();
        }
        public Users(string email, string username)
        {
            this.Email = email;
            this.Username = username;
        }


        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsBanned { get; set; }
        public bool IsOnline { get; set; }
        public bool Provider { get; set; }

        public virtual ICollection<RefreshToken> RefreshToken { get; set; }
    }
}
