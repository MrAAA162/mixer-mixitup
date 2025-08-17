using MixItUp.API.V1.Models;
using MixItUp.Base;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services;
using MixItUp.Base.Model; // For StreamingPlatformTypeEnum
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;

namespace MixItUp.WPF.Services.DeveloperAPI.V1
{
    [RoutePrefix("api/chat")]
    [Obsolete("This v1 API controller is deprecated. Please use the V2 API.")]
    public class ChatV1Controller : ApiController
    {
        // [DEPRECATED] This controller is deprecated and will be removed in a future release. Please use the V2 API endpoints.
        [Obsolete("This v1 API controller is deprecated. Please use the V2 API.")]
        [Route("users")]
        [HttpGet]
        public Task<IEnumerable<User>> GetChatUsers()
        {
            List<User> users = new List<User>();

            var chatUsers = ServiceManager.Get<UserService>().GetActiveUsers();
            foreach (var chatUser in chatUsers)
            {
                users.Add(UserV1Controller.UserFromUserDataViewModel(chatUser));
            }

            return Task.FromResult<IEnumerable<User>>(users);
        }

        // [DEPRECATED] This controller is deprecated and will be removed in a future release. Please use the V2 API endpoints.
        [Obsolete("This v1 API controller is deprecated. Please use the V2 API.")]
        [Route("message")]
        [HttpDelete]
        public async Task ClearChat()
        {
            await ServiceManager.Get<ChatService>().ClearMessages(StreamingPlatformTypeEnum.All);
        }

        // [DEPRECATED] This controller is deprecated and will be removed in a future release. Please use the V2 API endpoints.
        [Obsolete("This v1 API controller is deprecated. Please use the V2 API.")]
        [Route("message")]
        [HttpPost]
        public async Task SendChatMessage([FromBody]SendChatMessage chatMessage)
        {
            if (chatMessage == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "Unable to parse chat message from POST body."}, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Invalid POST Body"
                };
                throw new HttpResponseException(resp);
            }

            await ServiceManager.Get<ChatService>().SendMessage(chatMessage.Message, StreamingPlatformTypeEnum.All, chatMessage.SendAsStreamer);
        }

        // [DEPRECATED] This controller is deprecated and will be removed in a future release. Please use the V2 API endpoints.
        [Obsolete("This v1 API controller is deprecated. Please use the V2 API.")]
        [Route("whisper")]
        [HttpPost]
        public async Task SendWhisper([FromBody]SendChatWhisper chatWhisper)
        {
            if (chatWhisper == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new  ObjectContent<Error>(new Error { Message = "Unable to parse chat whisper from POST body." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Invalid POST Body"
                };
                throw new HttpResponseException(resp);
            }

            await ServiceManager.Get<ChatService>().Whisper(chatWhisper.UserName, StreamingPlatformTypeEnum.All, chatWhisper.Message, chatWhisper.SendAsStreamer);
        }
    }
}
