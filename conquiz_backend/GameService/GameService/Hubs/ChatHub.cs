using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GameService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Hubs
{
    public interface IChatClient
    {
        Task ReceiveMessage(string message);
    }
    [Authorize]
    public class ChatHub : Hub<IChatClient>
    {
        private readonly IExampleService service;

        public ChatHub(IExampleService service)
        {
            this.service = service;
        }
        // Same as above but it's strongly typed without magic strings ("receivemessage") is defined
        public async Task SendMessage(string message)
        {
            var result = await service.DoSomething();
            await Clients.All.ReceiveMessage($"{message} | and the service injection is: {result}");
        }

        // Call only whoever called the method
        public async Task SendMessageToCaller(string message)
        {
            await Clients.Caller.ReceiveMessage(message);
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "SignalR Users");
            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SignalR Users");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
