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

namespace GameService.Services
{
    public interface IGameLobbyService
    {
        Task<GameInstance> CreateGameLobby();
        Task<GameInstance> FindPublicMatch();
        Task<GameInstance> JoinGameLobby(string lobbyUrl);
        Task<GameInstance> StartGame(GameInstance gameInstance = null);
    }

    /// <summary>
    /// Handles the lobby state of the game and starting a gameinstance
    /// </summary>
    public class GameLobbyService : DataService<DefaultModel>, IGameLobbyService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IMapGeneratorService mapGeneratorService;
        private readonly IMessageBusClient messageBus;
        private readonly Random r = new Random();
        private const string DefaultMap = GameService.DefaultMap;
        private const int DefaultTerritoryScore = 500;
        private const int DefaultCapitalScore = 1000;


        const int RequiredPlayers = GameService.RequiredPlayers;
        const int InvitationCodeLength = GameService.InvitationCodeLength;

        public GameLobbyService(IDbContextFactory<DefaultContext> _contextFactory, IHttpContextAccessor httpContextAccessor, IMapGeneratorService mapGeneratorService, IMessageBusClient messageBus) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.mapGeneratorService = mapGeneratorService;
            this.messageBus = messageBus;
        }



        // TODO
        // As owner remove someone from group
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

        public async Task<GameInstance> FindPublicMatch()
        {
            using var db = contextFactory.CreateDbContext();
            var userId = httpContextAccessor.GetCurrentUserId();
            try
            {
                await CanPersonJoin(userId);
            }
            catch (ExistingLobbyGameException game)
            {
                return game.ExistingGame;
            }

            var openPublicGames = await db.GameInstance
                .Include(x => x.Participants)
                .Where(x => x.GameType == GameType.PUBLIC && x.GameState == GameState.IN_LOBBY && x.Participants.Count() < RequiredPlayers)
                .ToListAsync();

            // No open public games
            // Create a new public lobby
            if(openPublicGames.Count() == 0)
            {
                var gameInstance = await CreateGameInstance(db, GameType.PUBLIC, userId);

                await db.AddAsync(gameInstance);
                await db.SaveChangesAsync();

                return await db.GameInstance
                    .Include(x => x.Participants)
                    .ThenInclude(x => x.Player)
                    .Where(x => x.Id == gameInstance.Id)
                    .FirstOrDefaultAsync();
            }

            // Add player to a random lobby
            var randomGameIndex = r.Next(0, openPublicGames.Count());
            var chosenLobby = openPublicGames[randomGameIndex];

            chosenLobby.Participants.Add(new Participants()
            {
                PlayerId = userId,
                Score = 0,
                AvatarName = GetRandomAvatar(chosenLobby)
            });


            db.Update(chosenLobby);
            await db.SaveChangesAsync();

            return await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.Id == chosenLobby.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<GameInstance> CreateGameInstance(DefaultContext db, GameType gameType, int userId)
        {
            var map = await db.Maps.Where(x => x.Name == DefaultMap).FirstOrDefaultAsync();

            if (map == null)
            {
                await mapGeneratorService.ValidateMap();
            }

            var invitationLink = GenerateInvCode();

            while (await db.GameInstance
                .Where(x => x.InvitationLink == invitationLink &&
                    (x.GameState == GameState.IN_LOBBY || x.GameState == GameState.IN_PROGRESS))
                .FirstOrDefaultAsync() != null)
            {
                invitationLink = GenerateInvCode();
            }

            var gameInstance = new GameInstance()
            {
                GameType = gameType,
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
                AvatarName = GetRandomAvatar(gameInstance)
            });

            return gameInstance;
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
            catch (ExistingLobbyGameException game)
            {
                return game.ExistingGame;
            }

            var gameInstance = await CreateGameInstance(db, GameType.PRIVATE, userId);

            await db.AddAsync(gameInstance);
            await db.SaveChangesAsync();

            return await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.Id == gameInstance.Id)
                .FirstOrDefaultAsync();
        }

        private string GenerateInvCode()
        {
            var invitationLink = "";
            for (var i = 0; i < InvitationCodeLength; i++)
            {
                invitationLink += r.Next(0, 9).ToString();
            }
            return invitationLink;
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


        public async Task<GameInstance> JoinGameLobby(string lobbyUrl)
        {
            using var db = contextFactory.CreateDbContext();

            var userId = httpContextAccessor.GetCurrentUserId();
            var gameInstance = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.InvitationLink == lobbyUrl && x.GameState == GameState.IN_LOBBY && x.GameType == GameType.PRIVATE)
                .FirstOrDefaultAsync();

            if (gameInstance == null)
                throw new JoiningGameException("The invitation link is invalid");


            if (gameInstance.Participants.Count() == RequiredPlayers)
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
                Score = 0,
                AvatarName = GetRandomAvatar(gameInstance)
            });

            db.Update(gameInstance);
            await db.SaveChangesAsync();

            return await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.Id == gameInstance.Id)
                .FirstOrDefaultAsync();
        }



        public async Task<GameInstance> StartGame(GameInstance gameInstance = null)
        {
            using var a = contextFactory.CreateDbContext();
            var userId = httpContextAccessor.GetCurrentUserId();


            if(gameInstance == null)
            {
                gameInstance = await a.GameInstance
                    .Include(x => x.Participants)
                    .ThenInclude(x => x.Player)
                    .Where(x => x.GameCreatorId == userId && x.GameState == GameState.IN_LOBBY)
                    .FirstOrDefaultAsync();
            }

            if (gameInstance == null)
                throw new ArgumentException("Game instance is null or has completed already");

            var allPlayers = gameInstance.Participants.ToList();

            if (allPlayers.Count != RequiredPlayers)
                throw new ArgumentException("Game instance doesn't contain 3 players. Can't start yet.");

            // Make sure no player is in another game
            foreach (var user in allPlayers)
            {
                if (user.Player.IsInGame)
                    throw new ArgumentException($"Can't start game. `{user.Player.Username}` is in another game currently.");
            }


            // Get default map id
            var mapId = await a.Maps.Where(x => x.Name == DefaultMap).Select(x => x.Id).FirstAsync();

            // Create the game territories from the map territory templates
            var gameTerritories = await CreateGameTerritories(a, mapId, gameInstance.Id);

            // Includes capitals
            var modifiedTerritories = await ChooseCapitals(a, gameTerritories, gameInstance.Participants.ToArray());

            // Create the rounds for the NEUTRAL attack stage of the game (until all territories are taken)
            var initialRounding = await CreateNeutralAttackRounding(mapId, allPlayers, gameInstance.Id);

            // Assign all object territories & rounds and change gamestate
            gameInstance.GameState = GameState.IN_PROGRESS;
            gameInstance.ObjectTerritory = gameTerritories;
            gameInstance.Rounds = initialRounding;
            gameInstance.GameRoundNumber = 1;

            a.Update(gameInstance);
            await a.SaveChangesAsync();


            // Send request to question service to generate questions in the background
            RequestQuestions(gameInstance.Id, initialRounding, true);

            return await CommonTimerFunc.GetFullGameInstance(gameInstance.Id, a);
        }

        private void RequestQuestions(int gameInstanceId, Round[] rounds, bool isNeutralGeneration = false)
        {
            // Request questions only for the initial multiple questions for neutral attacking order
            // After multiple choices are over, request a new batch for number questions for all untaken territories
            messageBus.RequestQuestions(new RequestQuestionsDto()
            {
                Event = "Question_Request",
                GameInstanceId = gameInstanceId,
                MultipleChoiceQuestionsRoundId = rounds
                    .Where(x => x.AttackStage == AttackStage.MULTIPLE_NEUTRAL)
                    .Select(x => x.Id)
                    .ToList(),
                NumberQuestionsRoundId = new List<int>(),
                IsNeutralGeneration = isNeutralGeneration,
            });
        }

        public async Task<Round[]> CreateNeutralAttackRounding(int mapId, List<Participants> allPlayers, int gameInstanceId)
        {
            var totalTerritories = await mapGeneratorService.GetAmountOfTerritories(mapId);

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
                foreach(var roundAttackerId in order.UserRoundAttackOrders[i])
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


        public async Task<ObjectTerritory[]> CreateGameTerritories(DefaultContext a, int mapId, int gameInstanceId)
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

        public async Task<ObjectTerritory[]> ChooseCapitals(DefaultContext a, ObjectTerritory[] allTerritories, Participants[] participants)
        {
            var capitals = new List<ObjectTerritory>();

            // Get 3 capitals
            while (capitals.Count() < RequiredPlayers)
            {
                var randomTerritory = allTerritories[r.Next(0, allTerritories.Count())];

                // Capital already here
                if (capitals.Contains(randomTerritory)) continue;

                var borders = await mapGeneratorService.GetBorders(randomTerritory.MapTerritoryId);

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


        public string[] Avatars = new string[3]
        {
            "penguinAvatar",
            "penguinAvatar2",
            "penguinAvatar3",
        };
        private string GetRandomAvatar(GameInstance game)
        {
            if (Avatars.Length < RequiredPlayers)
                throw new ArgumentException("There aren't enough avatars for every player. Contact an administrator.");

            var selectedAvatar = "";
            while (string.IsNullOrEmpty(selectedAvatar))
            {
                var randomAvatar = Avatars[r.Next(0, Avatars.Length)];

                var duplicate = game.Participants.FirstOrDefault(x => x.AvatarName == randomAvatar);
                if (duplicate != null) continue;

                selectedAvatar = randomAvatar;
            }
            return selectedAvatar;
        }
    }
}
