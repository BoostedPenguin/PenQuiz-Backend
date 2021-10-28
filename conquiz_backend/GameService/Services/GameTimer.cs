﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using GameService.Context;
using GameService.Hubs;
using GameService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace GameService.Services
{
    public interface IGameTimer
    {
        void TimerStart();
    }

    /// <summary>
    /// Handles all game instance timers and callbacks the appropriate GameControlService functions
    /// </summary>
    public class GameTimer : DataService<DefaultModel>, IGameTimer
    {
        
        private readonly IContextFactory contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        
        // GroupID<Game> | CurrentTimer 
        public static ConcurrentDictionary<string, Timer> GameTimers = new ConcurrentDictionary<string, Timer>();

        public GameTimer(IContextFactory _contextFactory, IHttpContextAccessor httpContextAccessor, IHubContext<GameHub, IGameHub> hubContext) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.hubContext = hubContext;
        }

        public void TimerStart()
        {
            return;
            if(!GameTimers.ContainsKey("SomeKey"))
            {
                //GameTimers.TryGetValue("wawd", out Timer tryGet);
                //tryGet.Close();
            }
            var b = new Timer
            {
                Enabled = false,
                Interval = 5000,
                AutoReset = true
            };
            b.Elapsed += B_Elapsed;
            b.Start();
        }

        private void B_Elapsed(object sender, ElapsedEventArgs e)
        {
            hubContext.Clients.All.TESTING("Call me again in 5000ms");
        }
    }
}