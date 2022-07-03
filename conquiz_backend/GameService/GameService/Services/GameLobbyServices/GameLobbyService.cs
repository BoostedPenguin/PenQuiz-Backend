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
        Task<GameInstance> AddGameBot();
        Task<OnJoinLobbyResponse> CreateGameLobby();
        Task<OnJoinLobbyResponse> FindPublicMatch();
        Task<OnJoinLobbyResponse> JoinGameLobby(string lobbyUrl);
        
        // Game lobby character selection
        
        Task<RemovePlayerFromLobbyResponse> RemovePlayerFromLobby(int playerId);
        Task<LobbyParticipantCharacterResponse> LockInSelectedLobbyCharacter();
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

        public async Task<LobbyParticipantCharacterResponse> LockInSelectedLobbyCharacter()
        {
            using var db = contextFactory.CreateDbContext();
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);
         
            var gmLink = await db.GameInstance.FirstOrDefaultAsync(e => e.GameState == GameState.IN_LOBBY && e.Participants.Any(y => y.PlayerId == user.Id));

            return lobbyTimerService.PlayerLockCharacter(user.Id, gmLink.InvitationLink);
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

            // No open public games
            // Create a new public lobby
            if(openPublicGames.Count() == 0)
            {
                var gameInstance = await CreateGameInstance(db, GameType.PUBLIC, user);

                await db.AddAsync(gameInstance);
                await db.SaveChangesAsync();

                var aeUserCharacters = await GetThisUserAvailableCharacters(db, user.Id);

                var onRes = lobbyTimerService.AddPlayerToLobbyData(user.Id, aeUserCharacters.Select(e => e.Id).ToArray(), gameInstance.InvitationLink);

                return new OnJoinLobbyResponse(mapper.Map<GameLobbyDataResponse>(gameInstance), await GetAllCharacters(db), onRes);
            }

            // Add player to a random lobby
            var randomGameIndex = r.Next(0, openPublicGames.Count());
            var chosenLobby = openPublicGames[randomGameIndex];

            var newParticipant = await GenerateParticipant(db, chosenLobby.Participants.ToArray(), user.Id);

            chosenLobby.Participants.Add(newParticipant);


            db.Update(chosenLobby);
            await db.SaveChangesAsync();

            var lobbyResponse = mapper.Map<GameLobbyDataResponse>(chosenLobby);

            var availableUserCharacters = await GetThisUserAvailableCharacters(db, user.Id);

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






        public async Task<RemovePlayerFromLobbyResponse> RemovePlayerFromLobby(int playerId)
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

            var botParticipant = await GenerateParticipant(db, gm.Participants.ToArray(), gameBot.Id);

            botParticipant.Player = gameBot;

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
                return new OnJoinLobbyResponse(mapper.Map<GameLobbyDataResponse>(game.ExistingGame), game.AvailableUserCharacters, game.LobbyParticipantCharacterResponse);
            }

            var newParticipant = await GenerateParticipant(db, gameInstance.Participants.ToArray(), user.Id);

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
