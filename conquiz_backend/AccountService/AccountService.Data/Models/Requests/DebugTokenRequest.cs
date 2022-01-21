using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountService.Data.Models.Requests
{
    public class DebugTokenRequest
    {
        [Required]
        public string AccessToken { get; set; }
    }
}
