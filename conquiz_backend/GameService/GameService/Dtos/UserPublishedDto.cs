using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos
{
    public class UserPublishedDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public bool IsInGame { get; set; }
        public string Event { get; set; }
    }
}
