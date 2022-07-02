using GameService.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Timers;

namespace GameService.Services.GameTimerServices
{
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

}
