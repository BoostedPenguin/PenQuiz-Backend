using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos.SignalR_Responses
{
    public class SelectedTerritoryResponse
    {
        public string GameLink { get; set; }
        public int TerritoryId { get; set; }
        public int AttackedById { get; set; }
    }
}
