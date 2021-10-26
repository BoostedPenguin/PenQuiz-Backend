using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Dtos
{
    public class UserCreatedDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public bool IsInGame { get; set; }
        public string Event { get; set; }
    }
}
