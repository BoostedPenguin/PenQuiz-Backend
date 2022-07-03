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
using GameService.Services.GameUserActions;

namespace GameService.Hubs
{
    public interface IGameHub
    {
        Task GameStarting();

        // Receives the full mapped game instance
        // Very expensive
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

        Task GetGameLobbyData(GameLobbyDataResponse response);


        // Game lobby available characters res
        Task GameLobbyGetAvailableCharacters(CharacterResponse[] characterResponses);
        Task GameLobbyGetTakenCharacters(LobbyParticipantCharacterResponse response);


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
        Task ShowRoundingAttacker(RoundingAttackerRes responseData);

        Task GetGameUserId(int userId);
        Task OnSelectedTerritory(SelectedTerritoryResponse selectedTerritoryResponse);
        Task CloseQuestionScreen();
        Task MCQuestionPreviewResult(MCPlayerQuestionAnswers previewResult);
        Task NumberQuestionPreviewResult(NumberPlayerQuestionAnswers previewResult);
        Task GetRoundQuestion(QuestionClientResponse question);
        Task GameSendCountDownSeconds(int secondsForAction);
        Task TESTING(string message);

        // Characters
        Task ScientistUseNumberHint(ScientistUseNumberHintResponse response);

        Task VikingUseFortifyCapital(VikingUseFortifyResponse characterResponse);

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
        private readonly ICharacterAbilityService characterAbilityService;
        private readonly IMapper mapper;

        public GameHub(IGameTimerService timer, 
            IGameService gameService, 
            ILogger<GameHub> logger,
            ICharacterAbilityService characterAbilityService,
            IMapper mapper,
            IGameLobbyService gameLobbyService,
            IGameControlService gameControlService)
        {
            this.timer = timer;
            this.gameService = gameService;
            this.logger = logger;
            this.characterAbilityService = characterAbilityService;
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

                // Show the available attack territories
                if(response.RoundingAttackerRes is not null)
                    await Clients.Caller.ShowRoundingAttacker(response.RoundingAttackerRes);

                if (response.QuestionClientResponse is not null)
                    await Clients.Caller.GetRoundQuestion(response.QuestionClientResponse);

                //if(response.MCPlayerQuestionAnswers is not null)
                //    await Clients.Caller.MCQuestionPreviewResult(response.MCPlayerQuestionAnswers);


                // Give a permanent state game character for this user to the frontend
                //await Clients.Caller.GetGameCharacter(response.GameCharacter);
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


        // Character abilities

        public async Task ScientistUseAbility()
        {
            try
            {
                var res = characterAbilityService.ScientistUseNumberHint();

                // Calls caller only because this user does NOT change anything related to question for other instance players
                await Clients.Caller.ScientistUseNumberHint(res);

            }
            catch(Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        public async Task VikingUseAbility()
        {
            try
            {
                var res = await characterAbilityService.VikingUseAbility();

                await Clients.Group(res.GameLink).VikingUseFortifyCapital(res);
            }
            catch (Exception ex )
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }


        public async Task WizardUseAbility()
        {
            try
            {
                var res = characterAbilityService.WizardUseAbility();

                // Calls caller only because this user does NOT change anything related to question for other instance players
                await Clients.Caller.WizardUseMultipleChoiceHint(res);
            }
            catch(Exception ex)
            {
                logger.LogInformation($"Wizard ability not used: {ex.Message}");
            }
        }

        // End of character abilities

        public async Task SelectTerritory(string mapTerritoryName)
        {
            try
            {
                var response = gameControlService.SelectTerritory(mapTerritoryName);

                await Clients.Group(response.GameLink).OnSelectedTerritory(response);
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

        public async Task AddGameBot()
        {
            try
            {
                var game = await gameLobbyService.AddGameBot();

                var res1 = mapper.Map<GameInstanceResponse>(game);

                await Clients.Group(game.InvitationLink).GetGameInstance(res1);
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }


        /// <summary>
        /// As of 21/05/2022 SignalR does NOT provide a way to remove a user from a group
        /// This method currently only works for BOT users
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public async Task RemovePlayerFromLobby(int playerId)
        {
            try
            {
                var game = await gameLobbyService.RemovePlayerFromLobby(playerId);

                var res1 = mapper.Map<GameInstanceResponse>(game.GameInstance);

                if(!string.IsNullOrEmpty(game.RemovedPlayerId))
                {
                    await Clients.User(game.RemovedPlayerId).LobbyCanceled("You were kicked from the game lobby!");

                    await Groups.RemoveFromGroupAsync(game.RemovedPlayerId, game.GameInstance.InvitationLink);
                }

                await Clients.Group(game.GameInstance.InvitationLink).GetGameInstance(res1);
            }
            catch(Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        public async Task FindPublicMatch()
        {
            try
            {
                var result = await gameLobbyService.FindPublicMatch();

                await Groups.AddToGroupAsync(Context.ConnectionId, result.GameLobbyDataResponse.InvitationLink);

                // Give lobby data to client
                await Clients.Group(result.GameLobbyDataResponse.InvitationLink).GetGameLobbyData(result.GameLobbyDataResponse);

                // Give out all characters as a response
                await Clients.Caller.GameLobbyGetAvailableCharacters(result.AvailableUserCharacters);

                //await Clients.Caller.GetGameCharacter(gameCharacterRes);
                await Clients.Caller.NavigateToLobby();
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }


        /// <summary>
        /// Create PRIVATE game lobby
        /// </summary>
        /// <returns></returns>
        public async Task CreateGameLobby()
        {
            try
            {
                var result = await gameLobbyService.CreateGameLobby();
                await Groups.AddToGroupAsync(Context.ConnectionId, result.GameLobbyDataResponse.InvitationLink);

                // Give lobby data to client
                await Clients.Group(result.GameLobbyDataResponse.InvitationLink).GetGameLobbyData(result.GameLobbyDataResponse);

                // Give out all characters as a response
                await Clients.Caller.GameLobbyGetAvailableCharacters(result.AvailableUserCharacters);

                await Clients.Group(result.GameLobbyDataResponse.InvitationLink).GameLobbyGetTakenCharacters(result.LobbyParticipantCharacterResponse);

                //await Clients.Caller.GetGameCharacter(gameCharacterResponse);
                await Clients.Caller.NavigateToLobby();
            }
            catch(Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        /// <summary>
        /// Join PRIVATE game lobby
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task JoinGameLobby(string code)
        {
            try
            {
                var result = await gameLobbyService.JoinGameLobby(code);
                await Groups.AddToGroupAsync(Context.ConnectionId, result.GameLobbyDataResponse.InvitationLink);


                // Give lobby data to client
                await Clients.Group(result.GameLobbyDataResponse.InvitationLink).GetGameLobbyData(result.GameLobbyDataResponse);

                // Give out all characters as a response
                await Clients.Caller.GameLobbyGetAvailableCharacters(result.AvailableUserCharacters);


                await Clients.Group(result.GameLobbyDataResponse.InvitationLink).GameLobbyGetTakenCharacters(result.LobbyParticipantCharacterResponse);

                //await Clients.Caller.GetGameCharacter(gameCharacterResponse);
                await Clients.Caller.NavigateToLobby();
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        public async Task SelectLobbyCharacter(int characterId)
        {
            try
            {
                var participantCharacters = await gameLobbyService.SelectLobbyCharacter(characterId);

                await Clients.Group(participantCharacters.InvitiationLink).GameLobbyGetTakenCharacters(participantCharacters);
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }

        public async Task LockInSelectedLobbyCharacter()
        {
            try
            {
                var participantCharacters = await gameLobbyService.LockInSelectedLobbyCharacter();

                await Clients.Group(participantCharacters.LobbyParticipantCharacterResponse.InvitiationLink).GameLobbyGetTakenCharacters(participantCharacters.LobbyParticipantCharacterResponse);

                // Give lobby data to client
                await Clients.Group(participantCharacters.LobbyParticipantCharacterResponse.InvitiationLink).GetGameLobbyData(participantCharacters.GameLobbyDataResponse);
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }
    }
}
