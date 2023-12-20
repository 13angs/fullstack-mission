using Microsoft.AspNetCore.Mvc;
using System;

namespace chat_sv.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private static List<Member> members = new List<Member>
        {
            new Member { Id = 1, Name = "Member 1", Avatar = "https://cdn.dribbble.com/users/3841177/screenshots/11950347/cartoon-avatar_2020__8_circle.png" },
            new Member { Id = 2, Name = "Member 2", Avatar = "https://www.gamer-hub.io/static/img/team/sam.png" },
            // Add more members as needed
        };

        private static List<Message> messages = new List<Message>();

        [HttpGet("members")]
        public ActionResult<IEnumerable<Member>> GetMembers()
        {
            return Ok(members);
        }

        [HttpGet("messages")]
        public ActionResult<IEnumerable<Message>> GetMessages()
        {
            return Ok(messages);
        }

        [HttpPost("sendMessage")]
        public ActionResult SendMessage(MessageRequest messageRequest)
        {
            if (messageRequest == null || string.IsNullOrWhiteSpace(messageRequest.Text) || messageRequest.MemberId <= 0)
            {
                return BadRequest("Invalid message request");
            }

            Member? selectedMember = members.Find(m => m.Id == messageRequest.MemberId);

            if (selectedMember == null)
            {
                return NotFound("Member not found");
            }

            messages.Add(new Message
            {
                MemberId = selectedMember.Id,
                Text = messageRequest.Text,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            return Ok();
        }
    }

    public class Member
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Avatar { get; set; }
    }

    public class Message
    {
        public int MemberId { get; set; }
        public string? Text { get; set; }
        public long Timestamp { get; set; }
    }

    public class MessageRequest
    {
        public int MemberId { get; set; }
        public string? Text { get; set; }
    }
}
