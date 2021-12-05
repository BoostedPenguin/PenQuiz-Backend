using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace GameService.Services.GameTimerServices
{
    public class TimerWrapper : Timer
    {
        public TimerData Data { get; set; }

        public TimerWrapper(int gameInstanceId, string gameLink)
        {
            Data = new TimerData(gameInstanceId, gameLink);
        }

        public class TimerData
        {
            public TimerData(int gameInstanceId, string gameLink)
            {
                GameInstanceId = gameInstanceId;
                GameLink = gameLink;
            }
            public int GameInstanceId { get; set; }
            public int LastNeutralMCRound { get; set; }
            public int LastNeutralNumberRound { get; set; }

            // This is the invitation link which also acts as a group ID for signalR
            public string GameLink { get; set; }
            public int CurrentGameRoundNumber { get; set; }
            public ActionState NextAction { get; set; }
        }
    }
}
