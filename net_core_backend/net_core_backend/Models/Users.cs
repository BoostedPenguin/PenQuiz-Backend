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


        public Users(string email, string fName, string lName, string hashedPassword)
        {
            this.Email = email;
            this.FirstName = fName;
            this.LastName = lName;
            this.HashedPassword = hashedPassword;
        }

        public bool Admin { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [JsonIgnore]
        public string HashedPassword { get; set; }
    }
}
