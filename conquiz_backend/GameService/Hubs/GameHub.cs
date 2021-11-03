using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using GameService.Models;
using GameService.Services;
using GameService.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GameService.Hubs
{
    public interface IGameHub
    {
        Task GameStarting();
        Task GetGameInstance(GameInstance instance);
        Task LobbyCanceled(string message = "");
        Task CallerLeftGame();
        Task PersonLeftGame(int userId);
        Task GameException(string message);
        Task NavigateToLobby();
        Task NavigateToGame();
        Task TESTING(string message);
    }
    [Authorize]
    public class GameHub : Hub<IGameHub>
    {
        private readonly IGameTimer timer;
        private readonly IGameService gameService;
        private readonly IGameLobbyService gameLobbyService;
        private readonly IHttpContextAccessor httpContext;

        public GameHub(IGameTimer timer, IGameService gameService, IHttpContextAccessor httpContext, IGameLobbyService gameLobbyService)
        {
            this.timer = timer;
            this.gameService = gameService;
            this.httpContext = httpContext;
            this.gameLobbyService = gameLobbyService;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                //timer.TimerStart();
                var gameInstance = await gameService.OnPlayerLoginConnection();

                // If there aren't any IN PROGRESS game instances for this player, don't send him anything
                if (gameInstance == null)
                {
                    await base.OnConnectedAsync();
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, gameInstance.InvitationLink);
                await Clients.Group(gameInstance.InvitationLink).GetGameInstance(gameInstance);

                await Clients.Caller.NavigateToGame();
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

        public async Task CreateGameLobby()
        {
            try
            {
                var result = await gameLobbyService.CreateGameLobby();
                await Groups.AddToGroupAsync(Context.ConnectionId, result.InvitationLink);
                //await Clients.Caller.GetGameInstance(result);

                //var users = result.Participants.Select(x => x.Player).ToArray();
                //var current = result.Participants.Where(x => x.PlayerId == httpContext.GetCurrentUserId()).FirstOrDefault();
                //await Clients.Group(result.InvitationLink).AllLobbyPlayers(users);
                await Clients.Group(result.InvitationLink).GetGameInstance(result);
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

                await Clients.Group(gameInstance.InvitationLink).GetGameInstance(gameInstance);

                await Clients.Group(gameInstance.InvitationLink).GameStarting();
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
                await Clients.Group(game.InvitationLink).GetGameInstance(game);
                await Clients.Caller.NavigateToLobby();
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameException(ex.Message);
            }
        }
    }
}
