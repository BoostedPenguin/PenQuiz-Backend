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
            
            this.Start();
        }

        public TimerWrapper(int gameInstanceId, string gameLink, string gameGlobalIdentifier)
        {
            Data = new TimerData(gameInstanceId, gameLink, gameGlobalIdentifier);
        }

        public class CountDownTimer : Timer
        {
            public CountDownTimer(IHubContext<GameHub, IGameHub> hubContext, string gameLink)
            {
                this.GameLink = gameLink;
                this.hubContext = hubContext;
                Elapsed += CountDownTimer_Elapsed;
            }

            public string GameLink { get; set; }
            public int MaxTime { get; set; }
            public DateTime StartTime { get; set; }
            private readonly IHubContext<GameHub, IGameHub> hubContext;

            public void StartCountDownTimer(int maxTimeMs)
            {
                // Prevent timer if maxtimems is less than 1 second
                if (maxTimeMs < 1000) return;

                Interval = 1000;
                AutoReset = true;

                // -1 to offset client to server response
                // Ensures client time to communicate response won't cause time out
                MaxTime = maxTimeMs / 1000 - 1;
                StartTime = DateTime.Now;
                Start();
            }

            private void CountDownTimer_Elapsed(object sender, ElapsedEventArgs e)
            {
                var timer = (CountDownTimer)sender;

                TimeSpan elapsedTime = DateTime.Now - timer.StartTime;

                var timeRemaining = timer.MaxTime - (int)Math.Round(elapsedTime.TotalSeconds);

                hubContext.Clients.Group(timer.GameLink)
                    .GameSendCountDownSeconds(timeRemaining <= 0 ? 0 : timeRemaining);

                if (timeRemaining <= 0)
                {
                    timer.AutoReset = false;
                    timer.Stop();
                }
            }
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
