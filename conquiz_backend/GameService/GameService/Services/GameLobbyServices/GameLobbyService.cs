using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using GameService.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameService.MessageBus;
using GameService.Dtos;
using GameService.Services.GameTimerServices;
using GameService.Data.Models;
using GameService.Data;

namespace GameService.Services.GameLobbyServices
{
    public interface IGameLobbyService
    {
        Task<GameInstance> AddGameBot();
        Task CreateDebugLobby();
        Task<GameInstance> CreateGameLobby();
        Task<OnJoinLobbyResponse> FindPublicMatch();
        Task<OnJoinLobbyResponse> JoinGameLobby(string lobbyUrl);
        Task<RemovePlayerFromLobbyResponse> RemovePlayerFromLobby(int playerId);
        Task<GameInstance> StartGame(GameInstance gameInstance = null);
    }

    /// <summary>
    /// Handles the lobby state of the game and starting a gameinstance
    /// </summary>
    public partial class GameLobbyService : IGameLobbyService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IMapGeneratorService mapGeneratorService;
        private readonly Random r = new Random();
        private const string DefaultMap = GameService.DefaultMap;
        private const int DefaultTerritoryScore = 500;
        private const int DefaultCapitalScore = 1000;
        private readonly IGameTimerService timer;


        const int RequiredPlayers = GameService.RequiredPlayers;
        const int InvitationCodeLength = GameService.InvitationCodeLength;

        public GameLobbyService(
            IGameTimerService timer, 
            IDbContextFactory<DefaultContext> _contextFactory, 
            IHttpContextAccessor httpContextAccessor, 
            IMapGeneratorService mapGeneratorService
            )
        {
            this.timer = timer;
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.mapGeneratorService = mapGeneratorService;
        }


        public async Task<OnJoinLobbyResponse> FindPublicMatch()
        {
            using var db = contextFactory.CreateDbContext();
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);

            try
            {
                await CanPersonJoin(user.Id);
            }
            catch (ExistingLobbyGameException game)
            {
                return new OnJoinLobbyResponse(game.ExistingGame, game.ExistingCharacter);
            }

            var openPublicGames = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(e => e.Player)
                .Include(e => e.Participants)
                .ThenInclude(e => e.GameCharacter)
                .ThenInclude(e => e.Character)
                .Where(x => x.GameType == GameType.PUBLIC && x.GameState == GameState.IN_LOBBY && x.Participants.Count() < RequiredPlayers)
                .ToListAsync();

            // No open public games
            // Create a new public lobby
            if(openPublicGames.Count() == 0)
            {
                var gameInstance = await CreateGameInstance(db, GameType.PUBLIC, user);

                await db.AddAsync(gameInstance);
                await db.SaveChangesAsync();

                return new OnJoinLobbyResponse(gameInstance);
            }

            // Add player to a random lobby
            var randomGameIndex = r.Next(0, openPublicGames.Count());
            var chosenLobby = openPublicGames[randomGameIndex];

            var newParticipant = await GenerateParticipant(db, chosenLobby.Participants.ToArray(), user.Id);

            chosenLobby.Participants.Add(newParticipant);


            db.Update(chosenLobby);
            await db.SaveChangesAsync();

            return new OnJoinLobbyResponse(chosenLobby, chosenLobby.Participants.Where(e => e.PlayerId == user.Id).Select(e => e.GameCharacter).FirstOrDefault());
        }

        public async Task CreateDebugLobby()
        {
            using var context = contextFactory.CreateDbContext();

            var gameInstance = new GameInstance()
            {
                GameGlobalIdentifier = Guid.NewGuid().ToString(),
                GameType = GameType.PUBLIC,
                GameState = GameState.IN_LOBBY,
                InvitationLink = "1241",
                GameCreatorId = 1,
                Mapid = 1,
                StartTime = DateTime.Now,
                QuestionTimerSeconds = 30,
            };
            var allParticip = new List<Participants>();


            allParticip.Add(await GenerateParticipant(context, allParticip.ToArray(), 1));
            allParticip.Add(await GenerateParticipant(context, allParticip.ToArray(), 2));
            allParticip.Add(await GenerateParticipant(context, allParticip.ToArray(), 3));


            await context.AddAsync(gameInstance);

            await context.SaveChangesAsync();


            // Create the game territories from the map territory templates
            var gameTerritories = await CreateGameTerritories(context, 1, gameInstance.Id);

            // Includes capitals
            var modifiedTerritories = await ChooseCapitals(context, gameTerritories, gameInstance.Participants.ToArray());

            // Create the rounds for the NEUTRAL attack stage of the game (until all territories are taken)
            var initialRounding = await CreateNeutralAttackRounding(context, 1, gameInstance.Participants.ToList(), gameInstance.Id);

            // Assign all object territories & rounds and change gamestate
            gameInstance.GameState = GameState.IN_PROGRESS;
            gameInstance.ObjectTerritory = gameTerritories;
            gameInstance.Rounds = initialRounding;
            gameInstance.GameRoundNumber = 1;

            context.Update(gameInstance);
            await context.SaveChangesAsync();

            timer.OnGameStart(await CommonTimerFunc.GetFullGameInstance(gameInstance.Id, context));
        }

        public async Task<GameInstance> CreateGameLobby()
        {
            // Create url-link for people to join // Random string in header?
            // CLOSE ALL OTHER IN_LOBBY OPEN INSTANCES BY THIS PLAYER

            using var db = contextFactory.CreateDbContext();
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);

            try
            {
                await CanPersonJoin(user.Id);
            }
            catch (ExistingLobbyGameException game)
            {
                return game.ExistingGame;
            }

            var gameInstance = await CreateGameInstance(db, GameType.PRIVATE, user);

            await db.AddAsync(gameInstance);
            await db.SaveChangesAsync();

            return gameInstance;
        }



        public async Task<RemovePlayerFromLobbyResponse> RemovePlayerFromLobby(int playerId)
        {
            using var db = contextFactory.CreateDbContext();

            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);


            // Get the game where this user is the owner and is currently in lobby
            var gm = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.GameState == GameState.IN_LOBBY && x.GameType == GameType.PRIVATE && x.GameCreatorId == user.Id)
                .FirstOrDefaultAsync();

            if (gm is null)
                throw new ArgumentException("You are not the host of this lobby and can't remove players.");

            var selectedUser = gm.Participants.Where(e => e.PlayerId == playerId).FirstOrDefault();

            if (selectedUser.PlayerId == gm.GameCreatorId)
                throw new ArgumentException("You are trying to remove yourself!");

            if (selectedUser is null)
            {
                if (!selectedUser.Player.IsBot)
                    throw new ArgumentException("You are trying to remove a person who isn't in the lobby!");


                // The given id was invalid, remove the first bot you find in lobby
                var anyBot = gm.Participants.Where(e => e.Player.IsBot).FirstOrDefault();

                // No bot present to remove
                if (anyBot is null) return new RemovePlayerFromLobbyResponse()
                {
                    GameInstance = gm,
                };

                db.Remove(anyBot);
            }
            else
            {
                db.Remove(selectedUser);
            }


            await db.SaveChangesAsync();

            return new RemovePlayerFromLobbyResponse()
            {
                GameInstance = gm,
                RemovedPlayerId = selectedUser.Player.UserGlobalIdentifier,
            };
        }


        public async Task<GameInstance> AddGameBot()
        {
            using var db = contextFactory.CreateDbContext();

            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);


            // Get the game where this user is the owner and is currently in lobby
            var gm = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Include(e => e.Participants)
                .ThenInclude(e => e.GameCharacter)
                .Where(x => x.GameState == GameState.IN_LOBBY && x.GameType == GameType.PRIVATE && x.GameCreatorId == user.Id)
                .FirstOrDefaultAsync();

            if (gm is null)
                throw new ArgumentException("You are not the host of this lobby and can't add bots.");


            var gameBot = await GetRandomGameBot(db);

            var botParticipant = await GenerateParticipant(db, gm.Participants.ToArray(), gameBot.Id);

            gm.Participants.Add(botParticipant);

            db.Update(gm);

            await db.SaveChangesAsync();

            return gm;
        }

        public async Task<OnJoinLobbyResponse> JoinGameLobby(string lobbyUrl)
        {
            using var db = contextFactory.CreateDbContext();

            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);

            var gameInstance = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Include(e => e.Participants)
                .ThenInclude(e => e.GameCharacter)
                .ThenInclude(e => e.Character)
                .Where(x => x.InvitationLink == lobbyUrl && x.GameState == GameState.IN_LOBBY && x.GameType == GameType.PRIVATE)
                .FirstOrDefaultAsync();

            if (gameInstance == null)
                throw new JoiningGameException("The invitation link is invalid");


            if (gameInstance.Participants.Count() == RequiredPlayers)
                throw new JoiningGameException("Sorry, this lobby has reached the max amount of players");

            try
            {
                await CanPersonJoin(user.Id);
            }
            catch (ExistingLobbyGameException game)
            {
                return new OnJoinLobbyResponse(game.ExistingGame, game.ExistingCharacter);
            }

            var newParticipant = await GenerateParticipant(db, gameInstance.Participants.ToArray(), user.Id);

            gameInstance.Participants.Add(newParticipant);

            db.Update(gameInstance);
            await db.SaveChangesAsync();

            return new OnJoinLobbyResponse(gameInstance, gameInstance.Participants.Where(e => e.PlayerId == user.Id).Select(e => e.GameCharacter).FirstOrDefault());
        }

        public async Task<GameInstance> StartGame(GameInstance gameInstance = null)
        {
            using var a = contextFactory.CreateDbContext();
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await a.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);


            if (gameInstance == null)
            {
                gameInstance = await a.GameInstance
                    .Include(x => x.Participants)
                    .ThenInclude(x => x.Player)
                    .Where(x => x.GameCreatorId == user.Id && x.GameState == GameState.IN_LOBBY)
                    .FirstOrDefaultAsync();
            }

            if (gameInstance == null)
                throw new ArgumentException("Game instance is null or has completed already");

            var allPlayers = gameInstance.Participants.ToList();

            if (allPlayers.Count != RequiredPlayers)
                throw new ArgumentException("Game instance doesn't contain 3 players. Can't start yet.");

            // Make sure no player is in another game
            foreach (var us in allPlayers)
            {
                if (us.Player.IsInGame)
                    throw new ArgumentException($"Can't start game. `{us.Player.Username}` is in another game currently.");
            }


            // Get default map id
            var mapId = await a.Maps.Where(x => x.Name == DefaultMap).Select(x => x.Id).FirstAsync();

            // Create the game territories from the map territory templates
            var gameTerritories = await CreateGameTerritories(a, mapId, gameInstance.Id);

            // Includes capitals
            var modifiedTerritories = await ChooseCapitals(a, gameTerritories, gameInstance.Participants.ToArray());

            // Create the rounds for the NEUTRAL attack stage of the game (until all territories are taken)
            var initialRounding = await CreateNeutralAttackRounding(a, mapId, allPlayers, gameInstance.Id);

            // Assign all object territories & rounds and change gamestate
            gameInstance.GameState = GameState.IN_PROGRESS;
            gameInstance.ObjectTerritory = gameTerritories;
            gameInstance.Rounds = initialRounding;
            gameInstance.GameRoundNumber = 1;

            a.Update(gameInstance);
            await a.SaveChangesAsync();

            return await CommonTimerFunc.GetFullGameInstance(gameInstance.Id, a);
        }

    }
}
