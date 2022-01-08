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

        
        public void StartTimer(ActionState nextAction)
        {
            Data.NextAction = nextAction;
            var intervalTime = GameActionsTime.GetTime(Data.NextAction);

            Interval = intervalTime;
            Data.CountDownTimer.StartCountDownTimer(intervalTime);
            this.Start();
        }

        public TimerWrapper(int gameInstanceId, string gameLink)
        {
            Data = new TimerData(gameInstanceId, gameLink);
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
                Interval = 1000;
                AutoReset = true;
                MaxTime = maxTimeMs / 1000;
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

                Console.WriteLine($"Time remaining: {(timeRemaining <= 0 ? 0 : timeRemaining)}");

                if (timeRemaining <= 0)
                {
                    timer.AutoReset = false;
                    timer.Stop();
                }
            }
        }

        public class TimerData
        {
            public TimerData(int gameInstanceId, string gameLink)
            {
                GameInstanceId = gameInstanceId;
                GameLink = gameLink;
            }
            public CountDownTimer CountDownTimer { get; set; }
            public int GameInstanceId { get; set; }
            public int LastNeutralMCRound { get; set; }
            public int LastNeutralNumberRound { get; set; }
            public int LastPvpRound { get; set; }

            // This is the invitation link which also acts as a group ID for signalR
            public string GameLink { get; set; }
            public int CurrentGameRoundNumber { get; set; }
            public ActionState NextAction { get; set; }
        }
    }
}
