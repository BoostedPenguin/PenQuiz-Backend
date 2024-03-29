﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using GameService.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GameService.Services.GameTimerServices;
using GameService.Data.Models;
using GameService.Data;
using AutoMapper;
using GameService.Dtos.SignalR_Responses;
using Microsoft.Extensions.Logging;
using GameService.Services.GameLobbyServices;

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
        Task CancelOngoingGames(DefaultContext db);
        Task<OnPlayerLoginResponse> OnPlayerLoginConnection();
        Task<PersonDisconnectedGameResult> PersonDisconnected();
    }

    [Serializable]
    public class ExistingLobbyGameException : Exception
    {
        public GameInstance ExistingGame { get; }
        public CharacterResponse[] AvailableUserCharacters { get; init; }
        public LobbyParticipantCharacterResponse LobbyParticipantCharacterResponse { get; }

        public ExistingLobbyGameException(GameInstance existingGame, string message, CharacterResponse[] availableUserCharacters, LobbyParticipantCharacterResponse lobbyParticipantCharacterResponse) : base(message)
        {
            this.ExistingGame = existingGame;
            AvailableUserCharacters = availableUserCharacters;
            LobbyParticipantCharacterResponse = lobbyParticipantCharacterResponse;
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
    public class BorderSelectedGameException : GameException
    {
        public BorderSelectedGameException(string message) : base(message)
        {

        }
    }

    [Serializable]
    public class AnswerSubmittedGameException : GameException
    {
        public AnswerSubmittedGameException(string message) : base(message)
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
    public class GameService : IGameService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<GameService> logger;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IGameLobbyTimerService gameLobbyTimerService;
        private readonly ICurrentStageQuestionService dataExtractionService;
        private readonly IMapper mapper;
        private readonly IGameTimerService gameTimerService;
        public const string DefaultMap = "Antarctica";

        public const int RequiredPlayers = 3;
        public const int InvitationCodeLength = 4;

        public GameService(IDbContextFactory<DefaultContext> _contextFactory, 
            IHttpContextAccessor httpContextAccessor, 
            ILogger<GameService> logger,
            IGameTerritoryService gameTerritoryService,
            IGameLobbyTimerService gameLobbyTimerService,
            ICurrentStageQuestionService dataExtractionService,
            IMapper mapper,
            IGameTimerService gameTimerService) 
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            this.gameTerritoryService = gameTerritoryService;
            this.gameLobbyTimerService = gameLobbyTimerService;
            this.dataExtractionService = dataExtractionService;
            this.mapper = mapper;
            this.gameTimerService = gameTimerService;
        }

        public async Task CancelOngoingGames(DefaultContext db)
        {
            var ongoingGames = await db.GameInstance
                .Include(x => x.Participants)
                .Where(x => x.GameState == GameState.IN_LOBBY || x.GameState == GameState.IN_PROGRESS)
                .ToListAsync();
            ongoingGames.ForEach(x => x.GameState = GameState.CANCELED);

            await db.SaveChangesAsync();
        }

        private RoundingAttackerRes GetCurrentRoundingAttackerRes(GameInstance currentGameInstance)
        {
            var currentRound = currentGameInstance.Rounds
                .FirstOrDefault(x => x.GameRoundNumber == currentGameInstance.GameRoundNumber);

            // User can only choose to attack during PVP Mc rounds or Neutral MC rounds
            if (currentRound.AttackStage is AttackStage.MULTIPLE_NEUTRAL)
            {
                var currentAttacker = currentRound.NeutralRound.TerritoryAttackers
                    .First(x => x.AttackOrderNumber == currentRound.NeutralRound.AttackOrderNumber);

                var availableTerritories = gameTerritoryService
                    .GetAvailableAttackTerritoriesNames(currentGameInstance, currentAttacker.AttackerId, currentGameInstance.Id, true);

                return new RoundingAttackerRes(currentAttacker.AttackerId, availableTerritories);
            }

            if (currentRound.AttackStage is AttackStage.MULTIPLE_PVP)
            {
                var currentAttacker = currentRound.PvpRound.AttackerId;

                var availableTerritories = gameTerritoryService
                    .GetAvailableAttackTerritoriesNames(currentGameInstance, currentAttacker, currentGameInstance.Id, false);

                return new RoundingAttackerRes(currentAttacker, availableTerritories);
            }

            return null;
        }

        public async Task<OnPlayerLoginResponse> OnPlayerLoginConnection()
        {
            // Checks if there are any in-progress games and tries to put him back in that state
            using var db = contextFactory.CreateDbContext();
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);


            var ongoingGames = gameTimerService.GameTimers.Where(e =>
                e.Data.GameInstance.GameState == GameState.IN_PROGRESS && e.Data.GameInstance.Participants
                    .Any(y => y.PlayerId == user.Id))
            .Select(e => e.Data.GameInstance)
            .ToList();

            // There shouldn't be more than 1 active game for a given player at any time.
            // This is a bug, cancel all games for this player
            if(ongoingGames.Count > 1)
            {
                ongoingGames.ForEach(x => x.GameState = GameState.CANCELED);
                db.Update(ongoingGames);
                await db.SaveChangesAsync();

                foreach(var gm in ongoingGames)
                {
                    gameTimerService.CancelGameTimer(gm);
                }

                throw new GameException("There shouldn't be more than 1 active game for a given player at any time");
            }

            if (ongoingGames.Count == 1)
            {
                var currentGameInstance = ongoingGames.First();
                var thisUser = currentGameInstance.Participants.First(x => x.PlayerId == user.Id);
                thisUser.IsAfk = false;

                var timerNextEvent = gameTimerService.GameTimers
                    .Where(e => e.Data.GameInstance == currentGameInstance)
                    .FirstOrDefault()
                    .Data
                    .NextAction;

                QuestionClientResponse currentStageQuestion = null;
                MCPlayerQuestionAnswers mCPlayerQuestionAnswers = null;
                // Check if the current round is either pvp or neutral
                var roundingAttackerResponse = GetCurrentRoundingAttackerRes(currentGameInstance);


                try
                {
                    // To check if the rest of the players are currently on a question screen, check the next event of the timer
                    // If the following event will be CLOSE_QUESTION, then you get the current stage

                    var closingQuestionEvents = new List<ActionState>()
                    {
                        ActionState.END_MULTIPLE_CHOICE_QUESTION,
                        ActionState.END_NUMBER_QUESTION,
                        ActionState.END_FINAL_PVP_NUMBER_QUESTION,
                        ActionState.END_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION,
                        ActionState.END_CAPITAL_PVP_NUMBER_QUESTION,
                        ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION,
                        ActionState.END_PVP_NUMBER_QUESTION,
                    };


                    if (closingQuestionEvents.Contains(timerNextEvent))
                        currentStageQuestion = dataExtractionService.GetCurrentStageQuestionResponse(currentGameInstance);

                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }

                //try
                //{
                //    var showingQuestionPreviewEvents = new List<ActionState>()
                //    {
                //        ActionState.OPEN_PLAYER_ATTACK_VOTING,
                //        ActionState.SHOW_PREVIEW_GAME_MAP,
                //        ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING
                //    };

                //    // MC Question preview
                //    // Check if timer next event is OPEN_PLAYER_ATTACK_VOTING, SHOW_PREVIEW_GAME_MAP, OPEN_PVP_PLAYER_ATTACK_VOTING
                //    if (showingQuestionPreviewEvents.Contains(timerNextEvent))
                //        mCPlayerQuestionAnswers = QuestionResponseResultService.GenerateMCQuestionPreviewResult(currentGameInstance);
                //}
                //catch(Exception ex)
                //{
                //    logger.LogError(ex.Message);
                //}


                // Send to user the game instance response // GetGameInstance
                var gameInstanceRes = mapper.Map<GameInstanceResponse>(currentGameInstance);

                var gameCharacter =
                    currentGameInstance.Participants.Where(e => e.PlayerId == user.Id).Select(e => e.GameCharacter).First();

                var gameCharacterRes = mapper.Map<GameCharacterResponse>(gameCharacter);

                return new OnPlayerLoginResponse(gameInstanceRes, gameCharacterRes, thisUser.PlayerId, 
                    roundingAttackerRes: roundingAttackerResponse,
                    questionClientResponse: currentStageQuestion,
                    mCPlayerQuestionAnswers: mCPlayerQuestionAnswers
                    );
            }

            return new OnPlayerLoginResponse(null, null, user.Id);
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

            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserGlobalIdentifier == globalUserId);

            // Get game instances that are "active" for this person
            var activeGames = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Where(x => x.Participants
                    .Any(x => x.PlayerId == user.Id) && (x.GameState == GameState.IN_LOBBY || x.GameState == GameState.IN_PROGRESS))
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

            var thisUser = gameInstance.Participants.First(x => x.PlayerId == user.Id);

            switch (activeGames[0].GameState)
            {
                case GameState.IN_LOBBY:

                    // On creator disconnect or lobby kill.
                    if (user.Id == gameInstance.GameCreatorId)
                    {
                        gameInstance.GameState = GameState.CANCELED;
                        gameLobbyTimerService.CancelGameLobbyTimer(user.Id, activeGames[0].InvitationLink);
                        //var removeAll = gameInstance.Participants.Where(x => x.PlayerId != userId).ToList();
                        //db.RemoveRange(removeAll);
                    }
                    else
                    {
                        gameLobbyTimerService.CancelGameLobbyTimer(user.Id, activeGames[0].InvitationLink);
                        db.Remove(thisUser);
                    }

                    break;

                case GameState.IN_PROGRESS:

                    thisUser.IsAfk = true;

                    var botUsersCount = gameInstance.Participants.Where(e => e.Player.IsBot).ToList().Count;

                    var afkUsersCount = gameInstance.Participants.Where(e => e.IsAfk).ToList().Count;

                    // If someone started a game with 2 bots and he goes afk, kill the lobby
                    // If more than 1 person left automatically close the lobby because you'd be playing vs 2 bots
                    if(botUsersCount > 1 || afkUsersCount > 1)
                    {
                        gameInstance.GameState = GameState.CANCELED;

                        gameTimerService.CancelGameTimer(gameInstance);

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
                user.Id,
                gameInstance.GameState,
                gameInstance.Participants.Count(x => x.IsAfk));
        }
    }
}
