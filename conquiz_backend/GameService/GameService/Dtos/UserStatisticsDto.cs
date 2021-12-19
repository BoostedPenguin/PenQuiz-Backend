using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Dtos
{
    public class UserStatisticsDto
    {
        public int GamesWon { get; set; }
        public int TotalGames { get; set; }
        public string WinPercentage { get; set; }
    }
}
