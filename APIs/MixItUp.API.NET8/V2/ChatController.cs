using Microsoft.AspNetCore.Mvc;
using MixItUp.API.V2.Models;
using MixItUp.Base.Model;
using MixItUp.Base.Services;
using System;
using System.Threading.Tasks;

namespace MixItUp.API.NET8.V2
{
    [ApiController]
    [Route("api/v2/chat")]
    public class ChatController : ControllerBase
    {
        [HttpPost("message")]
        public async Task<IActionResult> SendChatMessage([FromBody] SendChatMessage chatMessage)
        {
            if (chatMessage == null)
            {
                return BadRequest("Missing chat message");
            }
            StreamingPlatformTypeEnum platform = StreamingPlatformTypeEnum.All;
            if (!string.IsNullOrEmpty(chatMessage.Platform) && !Enum.TryParse<StreamingPlatformTypeEnum>(chatMessage.Platform, ignoreCase: true, out platform))
            {
                return BadRequest($"Unknown platform: {chatMessage.Platform}");
            }
            await ServiceManager.Get<ChatService>().SendMessage(chatMessage.Message, platform, chatMessage.SendAsStreamer);
            return Ok();
        }

        [HttpPost("clear")]
        public async Task<IActionResult> ClearChat()
        {
            await ServiceManager.Get<ChatService>().ClearMessages(StreamingPlatformTypeEnum.All);
            return Ok();
        }
    }
}
