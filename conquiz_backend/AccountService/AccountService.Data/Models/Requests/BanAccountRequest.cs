using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountService.Data.Models.Requests
{
    public class BanAccountRequest
    {
        [Required]
        public int AccountId { get; set; }
    }
}
