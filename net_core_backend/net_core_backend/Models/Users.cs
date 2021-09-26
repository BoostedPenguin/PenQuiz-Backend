using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Users
    {
        public Users()
        {

        }

        public int Id { get; set; }

        public Users(string email, string username)
        {
            this.Email = email;
            this.Username = username;
        }

        public string Email { get; set; }
        public string Username { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsBanned { get; set; } = false;
        public bool IsOnline { get; set; }
        public bool Provider { get; set; }

        [JsonIgnore]
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
