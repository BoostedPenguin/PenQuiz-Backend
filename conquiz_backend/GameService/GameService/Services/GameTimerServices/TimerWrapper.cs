using GameService.Data.Models;
using GameService.Hubs;
using Microsoft.AspNetCore.SignalR;
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

        private DateTime EventStartAt;
        public double TimeUntilNextEvent => (EventStartAt - DateTime.Now).TotalMilliseconds;

        /// <summary>
        /// Override the existing interval with a new time
        /// </summary>
        /// <param name="overrideIntervalTime"></param>
        public void ChangeReminingInterval(int overrideIntervalTime)
        {
            // Need to stop the existing elapsing timer before you can restart it
            // Need to restart the countdowntimer as well
            this.Stop();
            Data.CountDownTimer.Stop();


            this.Interval = overrideIntervalTime;


            this.EventStartAt = DateTime.Now.AddMilliseconds(Interval);
            Data.CountDownTimer.StartCountDownTimer(overrideIntervalTime);
            this.Start();
        }

        public void StartTimer(ActionState nextAction, int? overrideIntervalTime = null)
        {
            Data.NextAction = nextAction;
            var intervalTime = GameActionsTime.GetTime(Data.NextAction);

            Interval = overrideIntervalTime ?? intervalTime;
            
            //Temp
            if (intervalTime != GameActionsTime.DefaultPreviewTime)
            {
                Data.CountDownTimer.StartCountDownTimer(overrideIntervalTime ?? intervalTime);
            }

            this.EventStartAt = DateTime.Now.AddMilliseconds(Interval);

            this.Start();
        }

        public TimerWrapper(int gameInstanceId, string gameLink, string gameGlobalIdentifier)
        {
            Data = new TimerData(gameInstanceId, gameLink, gameGlobalIdentifier);
        }


        public class TimerData
        {
            public TimerData(int gameInstanceId, string gameLink, string gameGlobalIdentifier)
            {
                GameInstanceId = gameInstanceId;
                GameLink = gameLink;
                GameGlobalIdentifier = gameGlobalIdentifier;
            }

            public Round GetBaseRound => GameInstance.Rounds.FirstOrDefault(e => e.GameRoundNumber == GameInstance.GameRoundNumber);

            public GameInstance GameInstance { get; set; }
            public CountDownTimer CountDownTimer { get; set; }
            public int GameInstanceId { get; set; }
            public int LastNeutralMCRound { get; set; }
            public int LastNeutralNumberRound { get; set; }
            public int LastPvpRound { get; set; }

            // This is the invitation link which also acts as a group ID for signalR
            public string GameLink { get; set; }
            public string GameGlobalIdentifier { get; set; }
            public ActionState NextAction { get; set; }
        }
    }
}
