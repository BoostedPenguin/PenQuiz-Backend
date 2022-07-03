using AutoMapper;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace GameService.Services.GameLobbyServices
{
    public interface IGameLobbyTimerService
    {
        LobbyParticipantCharacterResponse AddPlayerToLobbyData(int playerId, int[] ownedPlayerCharacterIds, string invitiationLink);
        void CancelGameLobbyTimer(int disconnectedPlayerId, string invitiationLink);
        LobbyParticipantCharacterResponse CreateGameLobbyTimer(string invitiationLink, int[] allCharacterIds, int creatorPlayerId, int[] ownedCreatorCharacterIds);
        LobbyParticipantCharacterResponse GetExistingLobbyParticipantCharacters(string invitationLink);
        LobbyParticipantCharacterResponse PlayerLockCharacter(int playerId, string invitiationLink);
        LobbyParticipantCharacterResponse PlayerSelectCharacter(int playerId, int characterId, string invitiationLink);
        //void RemovePlayerFromLobbyData(int playerId, string invitiationLink);
    }

    public class GameLobbyTimerService : IGameLobbyTimerService
    {
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IMapper mapper;
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IGameTimerService gameTimerService;
        private readonly IMapGeneratorService mapGeneratorService;
        private const int RequiredPlayers = 3;
        private const string DefaultMap = GameService.DefaultMap;
        private const int DefaultTerritoryScore = 500;
        private const int DefaultCapitalScore = 1000;
        private readonly Random r = new();
        private List<GameLobbyTimer> CurrentGameLobbies { get; set; }

        public GameLobbyTimerService(IHubContext<GameHub, IGameHub> hubContext,
            IMapper mapper,
            IDbContextFactory<DefaultContext> contextFactory, IGameTimerService gameTimerService, IMapGeneratorService mapGeneratorService)
        {
            this.hubContext = hubContext;
            this.mapper = mapper;
            this.contextFactory = contextFactory;
            this.gameTimerService = gameTimerService;
            this.mapGeneratorService = mapGeneratorService;
            this.CurrentGameLobbies = new List<GameLobbyTimer>();
        }

        /// <summary>
        /// When a user CREATES a lobby, add a reference to the timer list
        /// </summary>
        /// <param name="invitiationLink"></param>
        /// <param name=""></param>
        public LobbyParticipantCharacterResponse CreateGameLobbyTimer(string invitiationLink, int[] allCharacterIds, int creatorPlayerId, int[] ownedCreatorCharacterIds)
        {
            var gameLobbyData = new GameLobbyTimer(invitiationLink, allCharacterIds, creatorPlayerId, ownedCreatorCharacterIds, hubContext);
            gameLobbyData.Elapsed += StartGame;
            CurrentGameLobbies.Add(gameLobbyData);

            return gameLobbyData.GameLobbyData.GetParticipantCharactersResponse();
        }

        public LobbyParticipantCharacterResponse GetExistingLobbyParticipantCharacters(string invitationLink)
        {
            var gameLobby = CurrentGameLobbies.FirstOrDefault(e => e.GameLobbyData.GameCode == invitationLink);

            if (gameLobby == null)
                throw new ArgumentException("This lobby does not exist");

            return gameLobby.GameLobbyData.GetParticipantCharactersResponse();
        }

        public void CancelGameLobbyTimer(int disconnectedPlayerId, string invitiationLink)
        {
            var gameLobby = CurrentGameLobbies.FirstOrDefault(e => e.GameLobbyData.GameCode == invitiationLink);

            if (gameLobby == null)
                throw new ArgumentException("This lobby does not exist");

            gameLobby.Stop();

            gameLobby.GameLobbyData.RemoveParticipant(disconnectedPlayerId);
        }

        public LobbyParticipantCharacterResponse PlayerLockCharacter(int playerId, string invitiationLink)
        {
            var gameLobby = CurrentGameLobbies.FirstOrDefault(e => e.GameLobbyData.GameCode == invitiationLink);

            if (gameLobby == null)
                throw new ArgumentException("This lobby does not exist");

            gameLobby.GameLobbyData.ParticipantLockCharacter(playerId);

            return gameLobby.GameLobbyData.GetParticipantCharactersResponse();
        }

        public LobbyParticipantCharacterResponse PlayerSelectCharacter(int playerId, int characterId, string invitiationLink)
        {
            var gameLobby = CurrentGameLobbies.FirstOrDefault(e => e.GameLobbyData.GameCode == invitiationLink);

            if (gameLobby == null)
                throw new ArgumentException("This lobby does not exist");

            gameLobby.GameLobbyData.ParticipantSelectCharacter(playerId, characterId);

            return gameLobby.GameLobbyData.GetParticipantCharactersResponse();
        }

        public LobbyParticipantCharacterResponse AddPlayerToLobbyData(int playerId, int[] ownedPlayerCharacterIds, string invitiationLink)
        {
            var gameLobby = CurrentGameLobbies.FirstOrDefault(e => e.GameLobbyData.GameCode == invitiationLink);

            if (gameLobby == null)
                throw new ArgumentException("This lobby does not exist");

            gameLobby.GameLobbyData.AddInitialParticipant(playerId, ownedPlayerCharacterIds);


            // Required player amount, begin countdown for starting game
            if (gameLobby.GameLobbyData.GetParticipantCharacters().Length == 3)
            {
                gameLobby.StartTimer();
            }

            return gameLobby.GameLobbyData.GetParticipantCharactersResponse();
        }


        private async void StartGame(object sender, ElapsedEventArgs e)
        {
            using var a = contextFactory.CreateDbContext();
            //var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            //var user = await a.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);
            var lobbyTimer = (GameLobbyTimer)sender;
            lobbyTimer.AutoReset = false;
            lobbyTimer.Stop();
            lobbyTimer.CountDownTimer.AutoReset = false;
            lobbyTimer.CountDownTimer.Stop();

            var gameInstance = await a.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.InvitationLink == lobbyTimer.GameLobbyData.GameCode && x.GameState == GameState.IN_LOBBY)
                .FirstOrDefaultAsync();


            // Assign non-locked players a character
            var allCharacters = await a.Characters.ToListAsync();

            var participantCharacters = lobbyTimer.GameLobbyData.SelectCharactersForNonlockedPlayers();

            foreach(var participantCharacter in participantCharacters)
            {
                var currentParticipant = 
                    gameInstance.Participants.FirstOrDefault(e => e.PlayerId == participantCharacter.PlayerId);

                currentParticipant.GameCharacter = new GameCharacter(allCharacters.FirstOrDefault(e => e.Id == participantCharacter.CharacterId));
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
            var initialRounding = await CreateNeutralAttackRounding(a, mapId, allPlayers);

            // Assign all object territories & rounds and change gamestate
            gameInstance.GameState = GameState.IN_PROGRESS;
            gameInstance.ObjectTerritory = gameTerritories;
            gameInstance.Rounds = initialRounding;
            gameInstance.GameRoundNumber = 1;

            a.Update(gameInstance);
            await a.SaveChangesAsync();

            var fullGame = await CommonTimerFunc.GetFullGameInstance(gameInstance.Id, a);
            var res2 = mapper.Map<GameInstanceResponse>(fullGame);

            await hubContext.Clients.Group(gameInstance.InvitationLink).GetGameInstance(res2);

            await hubContext.Clients.Group(gameInstance.InvitationLink).GameStarting();

            // Officially end lobby stage and start the game timer
            gameTimerService.OnGameStart(fullGame);


            // Cleanup lobby data
            CurrentGameLobbies.Remove(lobbyTimer);
            lobbyTimer.GameLobbyData = null;
            lobbyTimer.CountDownTimer.Dispose();
            lobbyTimer.Dispose();
        }


        private async Task<Round[]> CreateNeutralAttackRounding(DefaultContext context, int mapId, List<Participants> allPlayers)
        {
            var totalTerritories = await mapGeneratorService.GetAmountOfTerritories(context, mapId);

            var order = CommonTimerFunc.GenerateAttackOrder(allPlayers.Select(x => x.PlayerId).ToList(), totalTerritories, RequiredPlayers);

            // Create default rounds
            var finalRounds = new List<Round>();

            // Stores game round number for each round
            var gameRoundNumber = 1;

            // Full rounds
            for (var i = 0; i < order.UserRoundAttackOrders.Count(); i++)
            {
                var baseRound = new Round
                {
                    GameRoundNumber = gameRoundNumber++,
                    AttackStage = AttackStage.MULTIPLE_NEUTRAL,
                    Description = $"MultipleChoice question. Attacker vs NEUTRAL territory",
                    IsQuestionVotingOpen = false,
                    IsTerritoryVotingOpen = false,
                };

                baseRound.NeutralRound = new NeutralRound()
                {
                    AttackOrderNumber = 1,
                };

                var attackOrderNumber = 1;
                foreach (var roundAttackerId in order.UserRoundAttackOrders[i])
                {
                    baseRound.NeutralRound.TerritoryAttackers.Add(new AttackingNeutralTerritory()
                    {
                        AttackerId = roundAttackerId,
                        AttackOrderNumber = attackOrderNumber++,
                    });
                }

                finalRounds.Add(baseRound);
            }

            var result = finalRounds.ToArray();

            return result;
        }

        private static async Task<ObjectTerritory[]> CreateGameTerritories(DefaultContext a, int mapId, int gameInstanceId)
        {
            var originTerritories = await a.MapTerritory.Where(x => x.MapId == mapId).ToListAsync();

            var gameTer = new List<ObjectTerritory>();
            foreach (var ter in originTerritories)
            {
                gameTer.Add(new ObjectTerritory()
                {
                    TerritoryScore = DefaultTerritoryScore,
                    MapTerritoryId = ter.Id,
                    GameInstanceId = gameInstanceId,
                });
            }
            return gameTer.ToArray();
        }

        private async Task<ObjectTerritory[]> ChooseCapitals(DefaultContext a, ObjectTerritory[] allTerritories, Participants[] participants)
        {
            var capitals = new List<ObjectTerritory>();

            // Get 3 capitals
            while (capitals.Count() < RequiredPlayers)
            {
                var randomTerritory = allTerritories[r.Next(0, allTerritories.Count())];

                // Capital already here
                if (capitals.Contains(randomTerritory)) continue;

                var borders = await mapGeneratorService.GetBorders(a, randomTerritory.MapTerritoryId);

                var borderWithOtherCapitals = capitals.Where(x => borders.Any(y => y.Id == x.MapTerritoryId)).FirstOrDefault();

                if (borderWithOtherCapitals != null) continue;

                randomTerritory.TerritoryScore = DefaultCapitalScore;
                capitals.Add(randomTerritory);
            }

            // Give each capital to a random player
            while (capitals.Where(x => x.TakenBy != null).ToList().Count() < RequiredPlayers)
            {
                var randomPlayer = participants[r.Next(0, RequiredPlayers)];
                if (capitals.Any(x => x.TakenBy == randomPlayer.PlayerId)) continue;

                var chosen = capitals.First(x => x.TakenBy == null);
                chosen.TakenBy = randomPlayer.PlayerId;
                chosen.IsCapital = true;
            }

            // TEST IF ALLTERRITORIES CHANGES OR NOT BASED ON REFERENCE !!!!

            return allTerritories;
        }
    }
}
