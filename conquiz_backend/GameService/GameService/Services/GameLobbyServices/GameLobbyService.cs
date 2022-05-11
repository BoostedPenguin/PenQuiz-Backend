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
        Task CreateDebugLobby();
        Task<GameInstance> CreateGameLobby();
        Task<GameInstance> FindPublicMatch();
        Task<GameInstance> JoinGameLobby(string lobbyUrl);
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


        public async Task<GameInstance> FindPublicMatch()
        {
            using var db = contextFactory.CreateDbContext();
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);

            try
            {
                await CanPersonJoin(user.Id);
            }
            catch (ExistingLobbyGameException game)
            {
                return game.ExistingGame;
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

                return gameInstance;
            }

            // Add player to a random lobby
            var randomGameIndex = r.Next(0, openPublicGames.Count());
            var chosenLobby = openPublicGames[randomGameIndex];

            var newParticipant = await GenerateParticipant(db, chosenLobby.Participants.ToArray(), user.Id);

            chosenLobby.Participants.Add(newParticipant);


            db.Update(chosenLobby);
            await db.SaveChangesAsync();

            return chosenLobby;
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
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);

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

        public async Task<GameInstance> JoinGameLobby(string lobbyUrl)
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
                return game.ExistingGame;
            }

            var newParticipant = await GenerateParticipant(db, gameInstance.Participants.ToArray(), user.Id);

            gameInstance.Participants.Add(newParticipant);

            db.Update(gameInstance);
            await db.SaveChangesAsync();

            return gameInstance;
        }

        public async Task<GameInstance> StartGame(GameInstance gameInstance = null)
        {
            using var a = contextFactory.CreateDbContext();
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await a.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);


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
