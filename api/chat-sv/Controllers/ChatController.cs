using chat_sv.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace chat_sv.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IMongoCollection<User> _usersCollection;  // Updated variable name
        private readonly IMongoCollection<Message> _messagesCollection;
        private readonly IHubContext<ChatHub> _chatHubContext;

        public ChatController(IMongoClient mongoClient, IHubContext<ChatHub> chatHubContext)
        {
            var database = mongoClient.GetDatabase("chat_db");
            _usersCollection = database.GetCollection<User>("users");  // Updated collection name
            _messagesCollection = database.GetCollection<Message>("messages");
            _chatHubContext = chatHubContext;
        }

        [HttpGet("users")]  // Updated endpoint
        public ActionResult<IEnumerable<User>> GetUsers()
        {
            var users = _usersCollection.Find(user => true).ToList();  // Updated variable name
            return Ok(users);
        }

        [HttpPost("sendMessage")]
        public async Task<ActionResult> SendMessage(MessageRequest messageRequest)
        {
            if (messageRequest == null || string.IsNullOrWhiteSpace(messageRequest.Text) || string.IsNullOrEmpty(messageRequest.UserId))  // Updated property name
            {
                return BadRequest("Invalid message request");
            }

            User selectedUser = await _usersCollection.Find(u => u.Id == messageRequest.UserId).FirstOrDefaultAsync();  // Updated variable name

            if (selectedUser == null)
            {
                return NotFound("User not found");  // Updated message
            }

            var newMessage = new Message
            {
                UserId = selectedUser.Id,  // Updated property name
                Text = messageRequest.Text,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            await _messagesCollection.InsertOneAsync(newMessage);

            // Notify clients about the new message using SignalR
            await _chatHubContext.Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(newMessage));

            return Ok();
        }

        [HttpGet("messages")]
        public ActionResult<IEnumerable<Message>> GetUserMessages([FromQuery] MessageParams messageParams)
        {
            if (string.IsNullOrEmpty(messageParams.UserId))  // Updated property name
            {
                var messages = _messagesCollection.Find(message => true).ToList();
                return Ok(messages);
            }

            var userMessages = _messagesCollection.Find(message => message.UserId == messageParams.UserId).ToList();  // Updated property name

            if (userMessages == null || userMessages.Count == 0)
            {
                return NotFound("No messages found for the specified user");  // Updated message
            }

            return Ok(userMessages);
        }
    }

    public class User  // Updated class name
    {
        [BsonElement("_id")]
        [JsonProperty("_id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("name")]
        [JsonProperty("name")]
        public string? Name { get; set; }

        [BsonElement("avatar")]
        [JsonProperty("avatar")]
        public string? Avatar { get; set; }
    }

    public class Message
    {
        [BsonElement("_id")]
        [JsonProperty("_id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("user_id")]  // Updated property name
        [JsonProperty("user_id")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("text")]
        [JsonProperty("text")]
        public string? Text { get; set; }

        [BsonElement("timestamp")]
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public class MessageRequest
    {
        [BsonElement("user_id")]  // Updated property name
        [JsonProperty("user_id")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("text")]
        [JsonProperty("text")]
        public string? Text { get; set; }
    }

    public class MessageParams
    {
        [FromQuery(Name = "user_id")]  // Updated property name
        public string UserId { get; set; } = string.Empty;
    }
}
