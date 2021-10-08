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
        Task CancelOngoingGames();
        Task<GameInstance> CreateGameLobby();
        Task<GameInstance> JoinGameLobby(string lobbyUrl);
        Task<GameInstance> RemoveOnDisconnect(int personToRemoveID);
        Task<GameInstance> RemoveParticipantFromGame(int personToRemoveID);
        Task StartGame();
    }

    [Serializable]
    public class ExistingLobbyGameException : Exception
    {
        public GameInstance ExistingGame { get; }
        public ExistingLobbyGameException(GameInstance existingGame, string message) : base(message)
        {
            this.ExistingGame = existingGame;
        }
    }

    [Serializable]
    public class JoiningGameException : Exception
    {
        public JoiningGameException(string message) : base(message)
        {

        }
    }

    public class GameService : DataService<DefaultModel>, IGameService
    {
        private readonly IContextFactory contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IMapGeneratorService mapGeneratorService;
        private readonly Random r = new Random();
        private const string DefaultMap = "Antarctica";

        const int RequiredPlayers = 3;
        const int InvitationCodeLength = 4;

        public GameService(IContextFactory _contextFactory, IHttpContextAccessor httpContextAccessor, IMapGeneratorService mapGeneratorService) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.mapGeneratorService = mapGeneratorService;
        }

        public async Task<GameInstance> CreateGameLobby()
        {
            // Create url-link for people to join // Random string in header?
            // CLOSE ALL OTHER IN_LOBBY OPEN INSTANCES BY THIS PLAYER
            
            using var db = contextFactory.CreateDbContext();
            var userId = httpContextAccessor.GetCurrentUserId();

            try
            {
                await CanPersonJoin(userId);
            }
            catch(ExistingLobbyGameException game)
            {
                return game.ExistingGame;
            }

            var map = await db.Maps.Where(x => x.Name == DefaultMap).FirstOrDefaultAsync();


            if (map == null)
            {
                await mapGeneratorService.ValidateMap();
            }

            var invitationLink = "";
            for(var i = 0; i < InvitationCodeLength; i++)
            {
                invitationLink += r.Next(0, 9).ToString();
            }
            var gameInstance = new GameInstance()
            {
                GameState = GameState.IN_LOBBY,
                InvitationLink = invitationLink,
                GameCreatorId = userId,
                Map = map,
                StartTime = DateTime.Now,
                QuestionTimerSeconds = 30,
            };

            gameInstance.Participants.Add(new Participants() 
            {
                PlayerId = userId,
                Score = 0,
            });

            await db.AddAsync(gameInstance);
            await db.SaveChangesAsync();

            return await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.Id == gameInstance.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> CanPersonJoin(int userId)
        {
            using var db = contextFactory.CreateDbContext();

            var userGames = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => (x.GameState == GameState.IN_LOBBY || x.GameState == GameState.IN_PROGRESS) && x.Participants
                    .Any(y => y.PlayerId == userId))
                .ToListAsync();


            // IF THERE ARE ANY IN_PROGRESS INSTANCES WITH THIS PLAYER PREVENT LOBBY CREATION
            if (userGames.Any(x => x.GameState == GameState.IN_PROGRESS))
            {
                throw new JoiningGameException("You already have a game in progress. It has to finish first.");
            }

            var lobbyGames = userGames.Where(x => x.GameState == GameState.IN_LOBBY).ToList();

            // You shouldn't be able to participate in more than 1 lobby game open.
            // It happend because some error. Close all game lobbies.
            if (lobbyGames.Count() > 1)
            {
                lobbyGames.ForEach(x => x.GameState = GameState.CANCELED);

                db.UpdateRange(lobbyGames);
                await db.SaveChangesAsync();
                throw new JoiningGameException("Oops. There was an internal server error. Please, start a new game lobby");
            }


            // User participates already in an open lobby.
            // Redirect him to this instead of creating a new instance.
            if (lobbyGames.Count() == 1)
            {
                throw new ExistingLobbyGameException(lobbyGames[0], "User participates already in an open lobby");
            }

            return true;
        }

        public async Task CancelOngoingGames()
        {
            using var db = contextFactory.CreateDbContext();

            var ongoingGames = await db.GameInstance
                .Include(x => x.Participants)
                .Where(x => x.GameState == GameState.IN_LOBBY || x.GameState == GameState.IN_PROGRESS)
                .ToListAsync();
            ongoingGames.ForEach(x => x.GameState = GameState.CANCELED);

            await db.SaveChangesAsync();
        }

        public async Task<GameInstance> JoinGameLobby(string lobbyUrl)
        {
            using var db = contextFactory.CreateDbContext();

            var userId = httpContextAccessor.GetCurrentUserId();
            var gameInstance = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.InvitationLink == lobbyUrl && x.GameState == GameState.IN_LOBBY)
                .FirstOrDefaultAsync();

            if (gameInstance == null)
                throw new JoiningGameException("The invitation link is invalid");


            if (gameInstance.Participants.Count() > RequiredPlayers)
                throw new JoiningGameException("Sorry, this lobby has reached the max amount of players");

            try
            {
                await CanPersonJoin(userId);
            }
            catch (ExistingLobbyGameException game)
            {
                return game.ExistingGame;
            }


            gameInstance.Participants.Add(new Participants()
            {
                PlayerId = userId,
                Score = 0
            });

            db.Update(gameInstance);
            await db.SaveChangesAsync();

            return await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.Id == gameInstance.Id)
                .FirstOrDefaultAsync();
        }

        [Obsolete("Not storing whole url in db, just game inv prefix")]
        public string CreateInvitiationUrl()
        {
            var baseUrl = httpContextAccessor.HttpContext.Request.Host.Value;
            baseUrl = $"http://{baseUrl}/game/join/{Guid.NewGuid():N}";
            Guid.NewGuid().ToString("N");
            return baseUrl;
        }

        // TODO
        public async Task<GameInstance> RemoveParticipantFromGame(int personToRemoveId)
        {
            using var db = contextFactory.CreateDbContext();

            var userId = httpContextAccessor.GetCurrentUserId();
            var gameInstance = await db.GameInstance
                .Include(x => x.Participants)
                .Where(x => x.GameCreatorId == userId && x.GameState == GameState.IN_LOBBY)
                .FirstOrDefaultAsync();
            return null;
        }

        public async Task<GameInstance> RemoveOnDisconnect(int personToRemoveID)
        {
            using var db = contextFactory.CreateDbContext();

            var gameInstance = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.Participants
                    .Any(x => x.PlayerId == personToRemoveID) && x.GameState == GameState.IN_LOBBY)
                .FirstOrDefaultAsync();

            if (gameInstance == null)
                throw new ArgumentException("You aren't the owner of any games that are currently in lobby");

            var removePerson = gameInstance.Participants
                .Where(x => x.PlayerId == personToRemoveID)
                .FirstOrDefault();

            if (removePerson == null)
                throw new ArgumentException("This person isn't in your game lobby.");

            // On creator disconnect or lobby kill.
            if(personToRemoveID == gameInstance.GameCreatorId)
            {
                gameInstance.GameState = GameState.CANCELED;
                var removeAll = gameInstance.Participants.Where(x => x.PlayerId != personToRemoveID).ToList();
                db.RemoveRange(removeAll);
            }
            else
            {
                db.Remove(removePerson);
            }
            await db.SaveChangesAsync();

            return gameInstance;
        }

        public async Task StartGame()
        {
            using var a = contextFactory.CreateDbContext();
            var userId = httpContextAccessor.GetCurrentUserId();



            var gameInstance = await a.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.GameCreatorId == userId && x.GameState == GameState.IN_LOBBY)
                .FirstOrDefaultAsync();

            if (gameInstance == null) 
                throw new ArgumentException("Game instance is null or has completed already");

            var allPlayers = gameInstance.Participants.ToList();

            if (allPlayers.Count != RequiredPlayers) 
                throw new ArgumentException("Game instance doesn't contain 3 players. Can't start yet.");

            // Make sure no player is in another game
            foreach(var user in allPlayers)
            {
                if (user.Player.IsInGame) 
                    throw new ArgumentException($"Can't start game. `{user.Player.Username}` is in another game currently.");
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
            // Create ObjectTerritories = MapTerritories :)
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
            while (emptyTerritories >= RequiredPlayers)
            {
                var fullRound = new List<int>();

                while (fullRound.Count < RequiredPlayers)
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
