using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using conquiz_backend.Models;
using conquiz_backend.Services;
using conquiz_backend.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace conquiz_backend.Hubs
{
    public interface IGameHub
    {
        Task GetGameInstance(GameInstance instance);
        Task LobbyCanceled(string message = "");
        Task PersonLeft();
        Task GameException(string message);
        Task NavigateToLobby();
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
            var gameInstance = await gameService.PersonDisconnected();

            // Person wasn't in a game or lobby. Safely remove him.
            if(gameInstance == null)
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameInstance.InvitationLink);
            await Clients.Caller.PersonLeft();
            
            // User was the owner of the lobby. Cancel the lobby for all people
            if (gameInstance.GameState == GameState.CANCELED)
            {
                string message = "";
                if (gameInstance.Participants.Where(x => x.IsBot).Count() > 1)
                    message = "Two players left the game. Game canceled.";
                else
                    message = "The owner canceled the lobby.";

                await Clients.Group(gameInstance.InvitationLink).LobbyCanceled(message);
            }
            else
            {
                await Clients.Group(gameInstance.InvitationLink).GetGameInstance(gameInstance);
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
