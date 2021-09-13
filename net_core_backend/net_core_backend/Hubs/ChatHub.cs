using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace net_core_backend.Hubs
{
    public interface IChatClient
    {
        Task ReceiveMessage(string message);
    }
    [Authorize]
    public class ChatHub : Hub<IChatClient>
    {
        //public async Task SendMessage(string message)
        //{
        //    await Clients.All.SendAsync("ReceiveMessage", Context.User);
        //}

        // Same as above but it's strongly typed without magic strings ("receivemessage") is defined
        public async Task SendMessage(string message)
        {
            await Clients.All.ReceiveMessage(message);
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
