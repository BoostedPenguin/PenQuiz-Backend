using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountService.Data.Models.Requests
{
    public class RFTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}
