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
        Task AllLobbyPlayers(Users[] users);
        Task LobbyCanceled();
        Task JoiningGameException(string message);
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
            //await Groups.AddToGroupAsync(Context.ConnectionId, "SignalR Users");
            //await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = int.Parse(Context.User.Claims
                .Where(x => x.Type == ClaimTypes.NameIdentifier)
                .Select(x => x.Value)
                .FirstOrDefault());

            var gameInstance = await gameService.RemoveOnDisconnect(userId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameInstance.InvitationLink);

            if(gameInstance.GameState == GameState.CANCELED)
            {
                await Clients.Group(gameInstance.InvitationLink).LobbyCanceled();
            }
            else
            {
                var users = gameInstance.Participants.Select(x => x.Player).ToArray();
                await Clients.Group(gameInstance.InvitationLink).AllLobbyPlayers(users);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task CreateGameLobby()
        {
            try
            {
                var result = await gameService.CreateGameLobby();
                await Groups.AddToGroupAsync(Context.ConnectionId, result.InvitationLink);
                await Clients.Caller.GetGameInstance(result);

                var users = result.Participants.Select(x => x.Player).ToArray();
                //var current = result.Participants.Where(x => x.PlayerId == httpContext.GetCurrentUserId()).FirstOrDefault();
                await Clients.Group(result.InvitationLink).AllLobbyPlayers(users);
            }
            catch(Exception ex)
            {
                await Clients.Caller.JoiningGameException(ex.Message);
            }
        }

        public async Task JoinGameLobby(string code)
        {
            try
            {
                var game = await gameService.JoinGameLobby(code);

                await Groups.AddToGroupAsync(Context.ConnectionId, game.InvitationLink);
                await Clients.Caller.GetGameInstance(game);

                var users = game.Participants.Select(x => x.Player).ToArray();
                //var current = result.Participants.Where(x => x.PlayerId == httpContext.GetCurrentUserId()).FirstOrDefault();
                await Clients.Group(game.InvitationLink).AllLobbyPlayers(users);
            }
            catch(Exception ex)
            {
                await Clients.Caller.JoiningGameException(ex.Message);
            }
        }
    }
}
