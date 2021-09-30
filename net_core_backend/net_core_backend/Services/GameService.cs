using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using net_core_backend.Context;
using net_core_backend.Models;
using net_core_backend.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace net_core_backend.Services
{
    public interface IGameService
    {
        void AddParticipantToGame();
        Task CreateGameLobby();
        void RemoveParticipantFromGame();
        Task StartGame();
    }

    public class GameService : DataService<DefaultModel>, IGameService
    {
        private readonly IContextFactory contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IMapGeneratorService mapGeneratorService;
        private readonly Random r = new Random();
        private const string DefaultMap = "Antarctica";

        const int RequiredPlayers = 3;

        public GameService(IContextFactory _contextFactory, IHttpContextAccessor httpContextAccessor, IMapGeneratorService mapGeneratorService) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.mapGeneratorService = mapGeneratorService;
        }

        public async Task CreateGameLobby()
        {
            // Create url-link for people to join // Random string in header?
        }

        public void AddParticipantToGame()
        {

        }

        public void RemoveParticipantFromGame()
        {

        }

        public async Task StartGame()
        {
            using var a = contextFactory.CreateDbContext();
            var userId = httpContextAccessor.GetCurrentUserId();



            var gameInstance = await a.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.GameCreatorId == userId && x.InProgress)
                .FirstOrDefaultAsync();

            if (gameInstance == null) throw new ArgumentException("Game instance is null or has completed already");

            var allPlayers = gameInstance.Participants.ToList();

            if (allPlayers.Count != RequiredPlayers) throw new ArgumentException("Game instance doesn't contain 3 players. Can't start yet.");

            // Make sure no player is in another game
            foreach(var user in allPlayers)
            {
                if (user.Player.IsInGame) throw new ArgumentException($"Can't start game. `{user.Player.Username}` is in another game currently.");
            }

            // Get default map id
            var mapId = await a.Maps.Where(x => x.Name == DefaultMap).Select(x => x.Id).FirstAsync();

            var order = await GenerateAttackOrder(allPlayers.Select(x => x.PlayerId).ToList(), mapId);

            var totalMultiQuestionRounds = order.UserRoundAttackOrders.Count() * RequiredPlayers;

            // Create default rounds
            var finalRounds = new List<Rounds>();
            
            // Full rounds
            for (var i = 0; i < order.UserRoundAttackOrders.Count(); i++)
            {
                // Inner round
                foreach(var roundAttackerId in order.UserRoundAttackOrders[i])
                {
                    finalRounds.Add(new Rounds
                    {
                        AttackerId = roundAttackerId,
                        DefenderId = null,
                        Description = $"Attacker vs NEUTRAL territory",
                        RoundStage = RoundStage.NOT_STARTED,
                    });
                }
            }
            //TODO
            // Can't start game if any player is in another game
        }

        class UserAttackOrder
        {
            public List<List<int>> UserRoundAttackOrders { get; set; }
            public int TotalTerritories { get; set; }
            public int LeftTerritories { get; set; }

            public UserAttackOrder(List<List<int>> userRoundAttackOrders, int totalTerritories, int leftTerritories)
            {
                this.UserRoundAttackOrders = userRoundAttackOrders;
                this.TotalTerritories = totalTerritories;
                this.LeftTerritories = leftTerritories;
            }
        }

        private async Task<UserAttackOrder> GenerateAttackOrder(List<int> userIds, int mapId)
        {
            if (userIds.Count != RequiredPlayers) throw new ArgumentException("There must be a total of 3 people in a game!");

            var totalTerritories = await mapGeneratorService.GetAmountOfTerritories(mapId);

            // 1 3 2   3 2 1

            // Removing the capital territories;
            var emptyTerritories = totalTerritories - RequiredPlayers;

            if (emptyTerritories < RequiredPlayers) throw new ArgumentException("There are less than 3 territories left except the capital. Abort the game.");

            // Store 
            var attackOrder = new List<List<int>>();
            while (emptyTerritories >= 3)
            {
                var fullRound = new List<int>();

                while (fullRound.Count < 3)
                {
                    var person = userIds[r.Next(0, RequiredPlayers)];
                    if (!fullRound.Contains(person))
                    {
                        fullRound.Add(person);
                    }
                }
                attackOrder.Add(fullRound);
                emptyTerritories -= fullRound.Count();
            }

            return new UserAttackOrder(attackOrder, totalTerritories, emptyTerritories);
        }
    }
}
