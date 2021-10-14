using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using net_core_backend.Models;
using net_core_backend.Services;
using net_core_backend.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace net_core_backend.Hubs
{
    public interface IGameHub
    {
        Task GetGameInstance(GameInstance instance);
        Task LobbyCanceled();
        Task PersonLeft();
        Task GameException(string message);
        Task NavigateToLobby();
    }
    [Authorize]
    public class GameHub : Hub<IGameHub>
    {
        private readonly IGameService gameService;
        private readonly IHttpContextAccessor httpContext;

        public GameHub(IGameService gameService, IHttpContextAccessor httpContext)
        {
            this.gameService = gameService;
            this.httpContext = httpContext;
        }

        public override async Task OnConnectedAsync()
        {
            var b = Context.ConnectionId;
            var userId = int.Parse(Context.User.Claims
                .Where(x => x.Type == ClaimTypes.NameIdentifier)
                .Select(x => x.Value)
                .FirstOrDefault());
            var g = 5;
            //await Groups.AddToGroupAsync(Context.ConnectionId, "SignalR Users");
            //await base.OnConnectedAsync();
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
            var gameInstance = await gameService.RemoveCurrentPerson();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameInstance.InvitationLink);
            await Clients.Caller.PersonLeft();

            if (gameInstance.GameState == GameState.CANCELED)
            {
                await Clients.Group(gameInstance.InvitationLink).LobbyCanceled();
            }
            else
            {
                //var users = gameInstance.Participants.Select(x => x.Player).ToArray();
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
                var result = await gameService.CreateGameLobby();
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

        public async Task JoinGameLobby(string code)
        {
            try
            {
                var game = await gameService.JoinGameLobby(code);

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
