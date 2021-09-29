using Microsoft.AspNetCore.Http;
using net_core_backend.Context;
using net_core_backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace net_core_backend.Services
{
    public class GameService : DataService<DefaultModel>
    {
        private readonly IContextFactory contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;

        public GameService(IContextFactory _contextFactory, IHttpContextAccessor httpContextAccessor) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
        }

        public void CreateGameLobby()
        {
            // Create url-link for people to join // Random string in header?
        }

        public void AddParticipantToGame()
        {

        }

        public void RemoveParticipantFromGame()
        {

        }

        public void StartGame()
        {
            // Can't start game if any player is in another game
        }
    }
}
