using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using chat_sv.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace chat_sv.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Message> _messagesCollection;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly IConfiguration _configuration;

        public ChatController(IMongoClient mongoClient, IHubContext<ChatHub> chatHubContext, IConfiguration configuration)
        {
            var database = mongoClient.GetDatabase("chat_db");
            _usersCollection = database.GetCollection<User>("users");
            _messagesCollection = database.GetCollection<Message>("messages");
            _chatHubContext = chatHubContext;
            _configuration = configuration;
        }

        [HttpGet("users")]
        [Authorize]
        public ActionResult<IEnumerable<User>> GetUsers()
        {
            var users = _usersCollection.Find(user => true).ToList();
            return Ok(users);
        }

        [HttpPost("sendMessage")]
        [Authorize]
        public async Task<ActionResult> SendMessage(MessageRequest messageRequest)
        {
            if (messageRequest == null || string.IsNullOrWhiteSpace(messageRequest.Text) || string.IsNullOrEmpty(messageRequest.UserId))
            {
                return BadRequest("Invalid message request");
            }

            User selectedUser = await _usersCollection.Find(u => u.Id == messageRequest.UserId).FirstOrDefaultAsync();

            if (selectedUser == null)
            {
                return NotFound("User not found");
            }

            var newMessage = new Message
            {
                UserId = selectedUser.Id,
                Text = messageRequest.Text,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            await _messagesCollection.InsertOneAsync(newMessage);

            // Notify clients about the new message using SignalR
            await _chatHubContext.Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(newMessage));

            return Ok();
        }

        [HttpGet("messages")]
        [Authorize]
        public ActionResult<IEnumerable<Message>> GetUserMessages([FromQuery] MessageParams messageParams)
        {
            if (string.IsNullOrEmpty(messageParams.UserId))
            {
                var messages = _messagesCollection.Find(message => true).ToList();
                return Ok(messages);
            }

            var userMessages = _messagesCollection.Find(message => message.UserId == messageParams.UserId).ToList();

            if (userMessages == null || userMessages.Count == 0)
            {
                return NotFound("No messages found for the specified user");
            }

            return Ok(userMessages);
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(LoginRequest loginRequest)
        {
            var user = await AuthenticateUserAsync(loginRequest.Username, loginRequest.Password);
            if (user == null)
            {
                return Unauthorized("Invalid username or password");
            }

            var token = GenerateJwtToken(user);
            return Ok(token);
        }

        private async Task<User> AuthenticateUserAsync(string username, string password)
        {
            var user = await _usersCollection.Find(u => u.Username == username).FirstOrDefaultAsync();

            if (user != null && VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return user;
            }

            return null;
        }

        private bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            using (var hmac = new HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i])
                        return false;
                }
            }
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id),
                    // Add additional claims if needed
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class User
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [BsonElement("username")]
        [JsonProperty("username")]
        public string Username { get; set; }

        [BsonElement("password_hash")]
        [JsonProperty("password_hash")]
        public byte[] PasswordHash { get; set; }

        [BsonElement("password_salt")]
        [JsonProperty("password_salt")]
        public byte[] PasswordSalt { get; set; }

        [BsonElement("name")]
        [JsonProperty("name")]
        public string? Name { get; set; }

        [BsonElement("avatar")]
        [JsonProperty("avatar")]
        public string? Avatar { get; set; }
    }

    public class Message
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [BsonElement("user_id")]
        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [BsonElement("text")]
        [JsonProperty("text")]
        public string? Text { get; set; }

        [BsonElement("timestamp")]
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public class MessageRequest
    {
        [JsonProperty("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("text")]
        public string? Text { get; set; }
    }

    public class MessageParams
    {
        [FromQuery(Name = "user_id")]
        public string UserId { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
