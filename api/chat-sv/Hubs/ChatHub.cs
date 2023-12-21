using chat_sv.Controllers;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace chat_sv.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(Message message)
        {
            // ... (existing code for sending messages)
            // Broadcast the message to all clients
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}