using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using GameService.Models;
using GameService.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GameService.Services
{

    public class PersonDisconnectedGameResult
    {
        public PersonDisconnectedGameResult(string invitationLink, int disconnectedUserId, GameState gameState, int gameBotCount)
        {
            InvitationLink = invitationLink;
            DisconnectedUserId = disconnectedUserId;
            GameState = gameState;
            GameBotCount = gameBotCount;
        }
        public string InvitationLink { get; set; }
        public int GameBotCount { get; set; }
        public int DisconnectedUserId { get; set; }
        public GameState GameState { get; set; }
    }

    public interface IGameService
    {
        Task CancelOngoingGames();
        Task<GameInstance> OnPlayerLoginConnection();
        Task<PersonDisconnectedGameResult> PersonDisconnected();
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
    public class GameException : Exception
    {
        public GameException(string message) : base(message)
        {

        }
    }

    [Serializable]
    public class JoiningGameException : GameException
    {
        public JoiningGameException(string message) : base(message)
        {

        }
    }

    /// <summary>
    /// Handles people's connection to the games and canceling existing games
    /// </summary>
    public class GameService : DataService<DefaultModel>, IGameService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        public const string DefaultMap = "Antarctica";

        public const int RequiredPlayers = 3;
        public const int InvitationCodeLength = 4;

        public GameService(IDbContextFactory<DefaultContext> _contextFactory, IHttpContextAccessor httpContextAccessor) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
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

        public async Task<GameInstance> OnPlayerLoginConnection()
        {
            // Checks if there are any in-progress games and tries to put him back in that state
            using var db = contextFactory.CreateDbContext();
            var userId = httpContextAccessor.GetCurrentUserId();

            var ongoingGames = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Include(x => x.ObjectTerritory)
                .ThenInclude(x => x.MapTerritory)
                .Include(x => x.Rounds)
                .Where(x => x.GameState == GameState.IN_PROGRESS && x.Participants
                    .Any(y => y.PlayerId == userId))
                .ToListAsync();

            // There shouldn't be more than 1 active game for a given player at any time.
            // This is a bug, cancel all games for this player
            if(ongoingGames.Count() > 1)
            {
                ongoingGames.ForEach(x => x.GameState = GameState.CANCELED);
                db.Update(ongoingGames);
                await db.SaveChangesAsync();
                throw new GameException("There shouldn't be more than 1 active game for a given player at any time");
            }

            if (ongoingGames.Count() == 1)
            {
                var currentGameInstance = ongoingGames.First();
                var thisUser = currentGameInstance.Participants.First(x => x.PlayerId == userId);
                thisUser.IsBot = false;
                
                db.Update(thisUser);
                await db.SaveChangesAsync();
                
                return currentGameInstance;
            }

            return null;
        }


        [Obsolete("Not storing whole url in db, just game inv prefix")]
        public string CreateInvitiationUrl()
        {
            var baseUrl = httpContextAccessor.HttpContext.Request.Host.Value;
            baseUrl = $"http://{baseUrl}/game/join/{Guid.NewGuid():N}";
            Guid.NewGuid().ToString("N");
            return baseUrl;
        }


        /// <summary>
        /// Handles users on disconnect for different stages of a game or app.
        /// </summary>
        /// <returns></returns>
        public async Task<PersonDisconnectedGameResult> PersonDisconnected()
        {
            using var db = contextFactory.CreateDbContext();

            var userId = httpContextAccessor.GetCurrentUserId();
            
            // Get game instances that are "active" for this person
            var activeGames = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.Participants
                    .Any(x => x.PlayerId == userId) && (x.GameState == GameState.IN_LOBBY || x.GameState == GameState.IN_PROGRESS))
                .ToListAsync();

            // Person wasn't in any active games
            if (activeGames.Count() == 0)
                return null;

            // If the person participates in more than 1 lobby it's a bug. Kill all lobbies with that person.
            if (activeGames.Count() > 1)
            {
                activeGames.ForEach(x => x.GameState = GameState.CANCELED);
                db.Update(activeGames);
                await db.SaveChangesAsync();
                throw new GameException("There shouldn't be more than 1 active game for a given player at any time");
            }


            // Only single instance
            var gameInstance = activeGames[0];

            var thisUser = gameInstance.Participants.First(x => x.PlayerId == userId);

            switch (activeGames[0].GameState)
            {
                case GameState.IN_LOBBY:

                    // On creator disconnect or lobby kill.
                    if (userId == gameInstance.GameCreatorId)
                    {
                        gameInstance.GameState = GameState.CANCELED;
                        //var removeAll = gameInstance.Participants.Where(x => x.PlayerId != userId).ToList();
                        //db.RemoveRange(removeAll);
                    }
                    else
                    {
                        db.Remove(thisUser);
                    }

                    break;

                case GameState.IN_PROGRESS:
                    // TODO
                    // MAKE USER AS A BOT
                    // UNTIL HE COMES BACK

                    thisUser.IsBot = true;

                    // If more than 1 person left automatically close the lobby because you'd be playing vs 2 bots
                    if (gameInstance.Participants.Where(x => x.IsBot).ToList().Count() > 1)
                    {
                        gameInstance.GameState = GameState.CANCELED;
                        //var removeAll = gameInstance.Participants.Where(x => x.PlayerId != userId).ToList();

                        db.Update(gameInstance);
                    }
                    else
                    {
                        db.Update(thisUser);
                    }
                    break;

                default:
                    throw new GameException("Unknown error. Please contact an administrator.");
            }

            await db.SaveChangesAsync();

            return new PersonDisconnectedGameResult(
                gameInstance.InvitationLink,
                userId,
                gameInstance.GameState,
                gameInstance.Participants.Count(x => x.IsBot));
        }
    }
}
