using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using GameService.Services;
using GameService.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GameService.Dtos;
using GameService.Dtos.SignalR_Responses;
using GameService.Services.GameTimerServices;
using GameService.Data.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GameService.Data;
using GameService.Services.CharacterActions;
using AutoMapper;
using GameService.Services.GameLobbyServices;

namespace GameService.Hubs
{
    public interface IGameHub
    {
        Task GameStarting();
        Task GetGameInstance(GameInstanceResponse instance);
        Task LobbyCanceled(string message = "");
        Task CallerLeftGame();
        Task PersonLeftGame(int userId);
        Task PlayerRejoined(int userId);
        Task GameException(string message);
        Task BorderSelectedGameException (string message);
        Task AnswerSubmittedGameException(string message);
        Task NavigateToLobby();
        Task NavigateToGame();


        // Client game actions
        // Send by user
        Task PlayerAnsweredMCQuestion(int answerId);


        // Server game actions
        // Controlled by timer

        // We don't need to explicitly tell how much time to display something
        // Because it is controlled by server timer push events
        // Things that require displaying of a timer do require ex. time to vote on something
        Task Game_Show_Main_Screen();
        Task ShowGameMap();
        Task ShowRoundingAttacker(int userId, string[] availableAttackTerritoriesNames);

        Task GetGameUserId(int userId);
        Task OnSelectedTerritory(SelectedTerritoryResponse selectedTerritoryResponse);
        Task CloseQuestionScreen();
        Task MCQuestionPreviewResult(MCPlayerQuestionAnswers previewResult);
        Task NumberQuestionPreviewResult(NumberPlayerQuestionAnswers previewResult);
        Task GetRoundQuestion(QuestionClientResponse question);
        Task GameSendCountDownSeconds(int secondsForAction);
        Task TESTING(string message);

        Task WizardGetAbilityUsesLeft(int usesLeft);
        Task WizardUseMultipleChoiceHint(WizardUseMultipleChoiceHint useMultipleChoiceHint);
    }
    [Authorize]
    public class GameHub : Hub<IGameHub>
    {
        private readonly IGameTimerService timer;
        private readonly IGameService gameService;
        private readonly IGameLobbyService gameLobbyService;
        private readonly IGameControlService gameControlService;
        private readonly ILogger<GameHub> logger;
        private readonly IMapper mapper;

        public GameHub(IGameTimerService timer, 
            IGameService gameService, 
            ILogger<GameHub> logger,
            IMapper mapper,
            IGameLobbyService gameLobbyService,
            IGameControlService gameControlService)
        {
            this.timer = timer;
            this.gameService = gameService;
            this.logger = logger;
            this.mapper = mapper;
            this.gameLobbyService = gameLobbyService;
            this.gameControlService = gameControlService;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var globalUserId = Context.User.Claims
                    .Where(x => x.Type == ClaimTypes.NameIdentifier)
                    .Select(x => x.Value)
                    .FirstOrDefault();

                var response = await gameService.OnPlayerLoginConnection();

                await Clients.Caller.GetGameUserId(response.UserId);

                //timer.TimerStart();
                // If there aren't any IN PROGRESS game instances for this player, don't send him anything
                if (response.GameInstanceResponse == null)
                {
                    await base.OnConnectedAsync();
                    return;
                }

                await Clients.Caller.GetGameInstance(response.GameInstanceResponse);
                await Clients.Group(response.GameInstanceResponse.InvitationLink).PlayerRejoined(response.UserId);
                await Clients.Caller.NavigateToGame();
                
                await Groups.AddToGroupAsync(Context.ConnectionId, response.GameInstanceResponse.InvitationLink);

            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            //var userId = int.Parse(Context.User.Claims
            //    .Where(x => x.Type == ClaimTypes.NameIdentifier)
            //    .Select(x => x.Value)
            //    .FirstOrDefault());

            try
            {
                await RemoveCurrentPersonFromGame();
            }
            finally {
                await base.OnDisconnectedAsync(exception);
            }
        }

        private async Task RemoveCurrentPersonFromGame()
        {
            var personDisconnectedResult = await gameService.PersonDisconnected();

            // Person wasn't in a game or lobby. Safely remove him.
            if(personDisconnectedResult == null)
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, personDisconnectedResult.InvitationLink);
            await Clients.Caller.CallerLeftGame();
            
            // User was the owner of the lobby. Cancel the lobby for all people
            if (personDisconnectedResult.GameState == GameState.CANCELED)
            {
                string message;
                if (personDisconnectedResult.GameBotCount > 1)
                    message = "Two players left the game. Game canceled.";
                else
                    message = "The owner canceled the lobby.";

                await Clients.Group(personDisconnectedResult.InvitationLink).LobbyCanceled(message);
            }
            else
            {
                // If person left and game is still in progress because he was replaced by bot
                await Clients.Group(personDisconnectedResult.InvitationLink).PersonLeftGame(personDisconnectedResult.DisconnectedUserId);
            }
        }

        public async Task LeaveGameLobby()
        {
            try
            {
                await RemoveCurrentPersonFromGame();
            }
            catch(Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }
        Stopwatch stopwatch = new Stopwatch();


        public async Task WizardUseAbility()
        {
            try
            {
                await gameControlService.WizardUseAbility();
            }
            catch(Exception ex)
            {
                logger.LogInformation($"Wizard ability not used: {ex.Message}");
            }
        }

        public async Task SelectTerritory(string mapTerritoryName)
        {
            try
            {

                stopwatch.Restart();
                var response = gameControlService.SelectTerritory(mapTerritoryName);

                await Clients.Group(response.GameLink).OnSelectedTerritory(response);
                stopwatch.Stop();

                logger.LogInformation($"Time elapsed for selecting territory: {stopwatch.ElapsedMilliseconds}");
            }
            catch (BorderSelectedGameException ex)
            {
                await Clients.Caller.BorderSelectedGameException(ex.Message);
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        public async Task AnswerQuestion(string answerIdNumber)
        {
            try
            {
                gameControlService.AnswerQuestion(answerIdNumber);
            }
            catch(AnswerSubmittedGameException ex)
            {
                await Clients.Caller.AnswerSubmittedGameException(ex.Message);
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        public async Task FindPublicMatch()
        {
            try
            {
                var result = await gameLobbyService.FindPublicMatch();

                await Groups.AddToGroupAsync(Context.ConnectionId, result.InvitationLink);
                var res1 = mapper.Map<GameInstanceResponse>(result);

                await Clients.Group(result.InvitationLink).GetGameInstance(res1);
                await Clients.Caller.NavigateToLobby();


                // If lobby is full automatically start
                // Public lobbies don't have a "host"
                // To prevent stale lobbies
                if(result.Participants.Count() == 3)
                {
                    var gameInstance = await gameLobbyService.StartGame(result);
                    var res2 = mapper.Map<GameInstanceResponse>(result);

                    await Clients.Group(gameInstance.InvitationLink).GetGameInstance(res2);

                    await Clients.Group(gameInstance.InvitationLink).GameStarting();

                    timer.OnGameStart(gameInstance);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        public async Task CreateGameLobby()
        {
            try
            {
                var result = await gameLobbyService.CreateGameLobby();
                await Groups.AddToGroupAsync(Context.ConnectionId, result.InvitationLink);

                var res1 = mapper.Map<GameInstanceResponse>(result);

                await Clients.Group(result.InvitationLink).GetGameInstance(res1);
                await Clients.Caller.NavigateToLobby();
            }
            catch(Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        public async Task StartGame()
        {
            try
            {
                var gameInstance = await gameLobbyService.StartGame();

                var res1 = mapper.Map<GameInstanceResponse>(gameInstance);

                await Clients.Group(gameInstance.InvitationLink).GetGameInstance(res1);

                await Clients.Group(gameInstance.InvitationLink).GameStarting();

                timer.OnGameStart(gameInstance);
            }
            catch(Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        public async Task JoinGameLobby(string code)
        {
            try
            {
                var game = await gameLobbyService.JoinGameLobby(code);

                await Groups.AddToGroupAsync(Context.ConnectionId, game.InvitationLink);
                //await Clients.Caller.GetGameInstance(game);

                //var users = game.Participants.Select(x => x.Player).ToArray();
                //var current = result.Participants.Where(x => x.PlayerId == httpContext.GetCurrentUserId()).FirstOrDefault();
                //await Clients.Group(game.InvitationLink).AllLobbyPlayers(users);
                var res1 = mapper.Map<GameInstanceResponse>(game);

                await Clients.Group(game.InvitationLink).GetGameInstance(res1);
                await Clients.Caller.NavigateToLobby();
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }
    }
}
