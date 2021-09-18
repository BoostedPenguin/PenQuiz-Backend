using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class Users : DefaultModel
    {
        public Users()
        {

        }


        public Users(string email, string username)
        {
            this.Email = email;
            this.Username = username;
        }

        public bool Admin { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }

        [JsonIgnore]
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
