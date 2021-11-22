using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Models.Requests
{
    public class RevokeTokenRequest
    {
        public string Token { get; set; }
    }
}
