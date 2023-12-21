using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace chat_sv.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IMongoCollection<Member> _membersCollection;
        private readonly IMongoCollection<Message> _messagesCollection;

        public ChatController(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("chat_db");
            _membersCollection = database.GetCollection<Member>("members");
            _messagesCollection = database.GetCollection<Message>("messages");
        }

        [HttpGet("members")]
        public ActionResult<IEnumerable<Member>> GetMembers()
        {
            var members = _membersCollection.Find(member => true).ToList();
            return Ok(members);
        }


        [HttpPost("sendMessage")]
        public ActionResult SendMessage(MessageRequest messageRequest)
        {
            if (messageRequest == null || string.IsNullOrWhiteSpace(messageRequest.Text) || string.IsNullOrEmpty(messageRequest.MemberId))
            {
                return BadRequest("Invalid message request");
            }

            Member selectedMember = _membersCollection.Find(m => m.Id == messageRequest.MemberId).FirstOrDefault();

            if (selectedMember == null)
            {
                return NotFound("Member not found");
            }

            _messagesCollection.InsertOne(new Message
            {
                MemberId = selectedMember.Id,
                Text = messageRequest.Text,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            return Ok();
        }

        [HttpGet("messages")]
        public ActionResult<IEnumerable<Message>> GetUserMessages([FromQuery] MessageParams messageParams)
        {
            if (string.IsNullOrEmpty(messageParams.MemberId))
            {
                var messages = _messagesCollection.Find(message => true).ToList();
                return Ok(messages);
            }

            var userMessages = _messagesCollection.Find(message => message.MemberId == messageParams.MemberId).ToList();

            if (userMessages == null || userMessages.Count == 0)
            {
                return NotFound("No messages found for the specified member");
            }

            return Ok(userMessages);
        }

    }

    public class Member
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

        [BsonElement("member_id")]
        [JsonProperty("member_id")]
        public string MemberId { get; set; } = string.Empty;

        [BsonElement("text")]
        [JsonProperty("text")]
        public string? Text { get; set; }

        [BsonElement("timestamp")]
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public class MessageRequest
    {
        [BsonElement("member_id")]
        [JsonProperty("member_id")]
        public string MemberId { get; set; } = string.Empty;

        [BsonElement("text")]
        [JsonProperty("text")]
        public string? Text { get; set; }
    }
    public class MessageParams
    {
        [FromQuery(Name = "member_id")]
        public string MemberId { get; set; } = string.Empty;
    }
}
