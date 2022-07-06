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
using AutoMapper;
using System.Timers;
using GameService.Dtos.SignalR_Responses;

namespace GameService.Services.GameLobbyServices
{
    public interface IGameLobbyService
    {
        Task<OnJoinLobbyResponse> AddGameBot();
        Task<OnJoinLobbyResponse> CreateGameLobby();
        Task<OnJoinLobbyResponse> FindPublicMatch();
        Task<OnJoinLobbyResponse> JoinGameLobby(string lobbyUrl);
        
        // Game lobby character selection
        
        Task<OnRemoveFromlobbyResponse> RemovePlayerFromLobby(int playerId);
        Task<GameDataWrapperResponse> LockInSelectedLobbyCharacter();
        Task<LobbyParticipantCharacterResponse> SelectLobbyCharacter(int characterId);
        //Task<GameInstance> StartGame(GameInstance gameInstance = null);
    }

    /// <summary>
    /// Handles the lobby state of the game and starting a gameinstance
    /// </summary>
    public partial class GameLobbyService : IGameLobbyService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IMapGeneratorService mapGeneratorService;
        private readonly IGameLobbyTimerService lobbyTimerService;
        private readonly IMapper mapper;
        private readonly Random r = new Random();
        private const string DefaultMap = GameService.DefaultMap;


        const int RequiredPlayers = GameService.RequiredPlayers;
        const int InvitationCodeLength = GameService.InvitationCodeLength;

        public GameLobbyService(
            IDbContextFactory<DefaultContext> _contextFactory, 
            IHttpContextAccessor httpContextAccessor, 
            IMapGeneratorService mapGeneratorService,
            IGameLobbyTimerService lobbyTimerService,
            IMapper mapper
            )
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.mapGeneratorService = mapGeneratorService;
            this.lobbyTimerService = lobbyTimerService;
            this.mapper = mapper;
        }

        public async Task<LobbyParticipantCharacterResponse> SelectLobbyCharacter(int characterId)
        {
            using var db = contextFactory.CreateDbContext();
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);

            var gmLink = await db.GameInstance.FirstOrDefaultAsync(e => e.GameState == GameState.IN_LOBBY && e.Participants.Any(y => y.PlayerId == user.Id));

            return lobbyTimerService.PlayerSelectCharacter(user.Id, characterId, gmLink.InvitationLink);
        }

        public async Task<GameDataWrapperResponse> LockInSelectedLobbyCharacter()
        {
            using var db = contextFactory.CreateDbContext();
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);
         
            var gm = await db.GameInstance
                .Include(e => e.Participants)
                .ThenInclude(e => e.Player)
                .Include(e => e.Participants)
                .ThenInclude(e => e.GameCharacter)
                .ThenInclude(e => e.Character)
                .FirstOrDefaultAsync(e => e.GameState == GameState.IN_LOBBY && e.Participants.Any(y => y.PlayerId == user.Id));

            var response = lobbyTimerService.PlayerLockCharacter(user.Id, gm.InvitationLink);

            var selectedCharacterId = response.ParticipantCharacters.FirstOrDefault(e => e.PlayerId == user.Id).CharacterId;

            var currentParticipant = gm.Participants.FirstOrDefault(e => e.PlayerId == user.Id);


            var character = await db.Characters.FirstOrDefaultAsync(e => e.Id == selectedCharacterId);
            currentParticipant.GameCharacter = new GameCharacter(character);

            db.Update(currentParticipant);
            await db.SaveChangesAsync();

            return new GameDataWrapperResponse()
            {
                GameLobbyDataResponse = mapper.Map<GameLobbyDataResponse>(gm),
                LobbyParticipantCharacterResponse = response,
            };
        }


        public async Task<OnJoinLobbyResponse> FindPublicMatch()
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
                return new OnJoinLobbyResponse(mapper.Map<GameLobbyDataResponse>(game.ExistingGame), game.AvailableUserCharacters, game.LobbyParticipantCharacterResponse); ;
            }

            var openPublicGames = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(e => e.Player)
                .Include(e => e.Participants)
                .ThenInclude(e => e.GameCharacter)
                .ThenInclude(e => e.Character)
                .Where(x => x.GameType == GameType.PUBLIC && x.GameState == GameState.IN_LOBBY && x.Participants.Count() < RequiredPlayers)
                .ToListAsync();


            var availableUserCharacters = await GetThisUserAvailableCharacters(db, user.Id);


            // No open public games
            // Create a new public lobby
            if (openPublicGames.Count() == 0)
            {
                var gameInstance = await CreateGameInstance(db, GameType.PUBLIC, user);

                await db.AddAsync(gameInstance);
                await db.SaveChangesAsync();


                var allCharacters = await GetAllCharacters(db);

                var timerRes = lobbyTimerService.CreateGameLobbyTimer(
                    gameInstance.InvitationLink,
                    allCharacters.Select(e => e.Id).ToArray(),
                    user.Id,
                    availableUserCharacters.Select(e => e.Id).ToArray());


                return new OnJoinLobbyResponse(mapper.Map<GameLobbyDataResponse>(gameInstance), allCharacters, timerRes);
            }

            // Add player to a random lobby
            var randomGameIndex = r.Next(0, openPublicGames.Count());
            var chosenLobby = openPublicGames[randomGameIndex];

            var newParticipant = GenerateParticipant(chosenLobby.Participants.ToArray(), user.Id);

            chosenLobby.Participants.Add(newParticipant);


            db.Update(chosenLobby);
            await db.SaveChangesAsync();

            var lobbyResponse = mapper.Map<GameLobbyDataResponse>(chosenLobby);

            var res = lobbyTimerService.AddPlayerToLobbyData(user.Id, availableUserCharacters.Select(e => e.Id).ToArray(), lobbyResponse.InvitationLink);

            return new OnJoinLobbyResponse(lobbyResponse, await GetAllCharacters(db), res);
        }

        public async Task<OnJoinLobbyResponse> CreateGameLobby()
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
                return new OnJoinLobbyResponse(mapper.Map<GameLobbyDataResponse>(game.ExistingGame), game.AvailableUserCharacters, game.LobbyParticipantCharacterResponse);
            }

            var gameInstance = await CreateGameInstance(db, GameType.PRIVATE, user);

            await db.AddAsync(gameInstance);
            await db.SaveChangesAsync();

            var availableUserCharacters = await GetThisUserAvailableCharacters(db, user.Id);

            var allCharacters = await GetAllCharacters(db);

            var res = lobbyTimerService.CreateGameLobbyTimer(
                gameInstance.InvitationLink,
                allCharacters.Select(e => e.Id).ToArray(), 
                user.Id, 
                availableUserCharacters.Select(e => e.Id).ToArray());

            var lobbyResponse = mapper.Map<GameLobbyDataResponse>(gameInstance);

            return new OnJoinLobbyResponse(lobbyResponse, allCharacters, res);
        }






        public async Task<OnRemoveFromlobbyResponse> RemovePlayerFromLobby(int playerId)
        {
            using var db = contextFactory.CreateDbContext();

            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);


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
                if (anyBot is null) return new OnRemoveFromlobbyResponse()
                {
                    InvitationLink = gm.InvitationLink,
                    RemovedPlayerId = anyBot.PlayerId
                };

                db.Remove(anyBot);
            }
            else
            {
                db.Remove(selectedUser);
            }


            lobbyTimerService.CancelGameLobbyTimer(selectedUser.PlayerId, gm.InvitationLink);

            await db.SaveChangesAsync();

            return new OnRemoveFromlobbyResponse()
            {
                InvitationLink = gm.InvitationLink,
                RemovedPlayerId = selectedUser.PlayerId,
            };
        }


        public async Task<OnJoinLobbyResponse> AddGameBot()
        {
            using var db = contextFactory.CreateDbContext();

            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);


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

            var botParticipant = GenerateParticipant(gm.Participants.ToArray(), gameBot.Id);

            var allCharacters = await db.Characters.ToArrayAsync();

            botParticipant.Player = gameBot;

            var randomCharacterId = lobbyTimerService.GetRandomAvailableCharacter(gm.InvitationLink);

            lobbyTimerService
                .AddPlayerToLobbyData(gameBot.Id, allCharacters.Select(e => e.Id).ToArray(), gm.InvitationLink);

            lobbyTimerService
                .PlayerSelectCharacter(gameBot.Id, randomCharacterId, gm.InvitationLink);

            var res = lobbyTimerService.PlayerLockCharacter(gameBot.Id, gm.InvitationLink);


            botParticipant.GameCharacter = 
                new GameCharacter(allCharacters.FirstOrDefault(e => e.Id == randomCharacterId));

            gm.Participants.Add(botParticipant);

            db.Update(gm);

            await db.SaveChangesAsync();

            return new OnJoinLobbyResponse(mapper.Map<GameLobbyDataResponse>(gm), mapper.Map<CharacterResponse[]>(allCharacters), res);
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
                return new OnJoinLobbyResponse(mapper.Map<GameLobbyDataResponse>(game.ExistingGame), game.AvailableUserCharacters, game.LobbyParticipantCharacterResponse);
            }

            var newParticipant = GenerateParticipant(gameInstance.Participants.ToArray(), user.Id);

            gameInstance.Participants.Add(newParticipant);

            db.Update(gameInstance);
            await db.SaveChangesAsync();


            var availableUserCharacters = await GetThisUserAvailableCharacters(db, user.Id);

            var res = lobbyTimerService.AddPlayerToLobbyData(user.Id, availableUserCharacters.Select(e => e.Id).ToArray(), gameInstance.InvitationLink);


            var lobbyResponse = mapper.Map<GameLobbyDataResponse>(gameInstance);

            return new OnJoinLobbyResponse(lobbyResponse, await GetAllCharacters(db), res);
        }
    }
}
