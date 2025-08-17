using Microsoft.AspNetCore.Mvc;
using MixItUp.API.V2.Models;
using MixItUp.Base;
using MixItUp.Base.Model;
using MixItUp.Base.Model.User;
using MixItUp.Base.Model.User.Platform;
using MixItUp.Base.Services;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MixItUp.API.NET8.V2
{
    [ApiController]
    [Route("api/v2/users")]
    public class UsersController : ControllerBase
    {
        [HttpGet("{userId:guid}")]
        public async Task<ActionResult<GetSingleUserResponse>> GetUserById(Guid userId)
        {
            await ServiceManager.Get<UserService>().LoadAllUserData();
            if (!ChannelSession.Settings.Users.TryGetValue(userId, out var user) || user == null)
            {
                return NotFound();
            }
            return Ok(new GetSingleUserResponse { User = UserMapper.ToUser(user) });
        }

        [HttpGet("{platform}/{usernameOrID}")]
        public async Task<ActionResult<GetSingleUserResponse>> GetUserByPlatformUsername(string platform, string usernameOrID)
        {
            await ServiceManager.Get<UserService>().LoadAllUserData();
            if (!Enum.TryParse<StreamingPlatformTypeEnum>(platform, ignoreCase: true, out var platformEnum))
            {
                return BadRequest($"Unknown platform: {platform}");
            }
            var usermodel = await ServiceManager.Get<UserService>().GetUserByPlatform(platformEnum, platformID: usernameOrID, platformUsername: usernameOrID, performPlatformSearch: true);
            if (usermodel == null)
            {
                return NotFound();
            }
            if (!ChannelSession.Settings.Users.TryGetValue(usermodel.ID, out var user) || user == null)
            {
                return NotFound();
            }
            return Ok(new GetSingleUserResponse { User = UserMapper.ToUser(user) });
        }

        [HttpGet]
        public async Task<ActionResult<GetListOfUsersResponse>> GetAllUsers([FromQuery] int skip = 0, [FromQuery] int pageSize = 25)
        {
            await ServiceManager.Get<UserService>().LoadAllUserData();
            var users = ChannelSession.Settings.Users.Values
                .OrderBy(u => u.ID)
                .Skip(skip)
                .Take(pageSize);
            var result = new GetListOfUsersResponse();
            result.TotalCount = ChannelSession.Settings.Users.Count;
            foreach (var user in users)
            {
                result.Users.Add(UserMapper.ToUser(user));
            }
            return Ok(result);
        }

        [HttpGet("active")]
        public async Task<ActionResult<GetListOfUsersResponse>> GetAllActiveUsers([FromQuery] int skip = 0, [FromQuery] int pageSize = 25)
        {
            await ServiceManager.Get<UserService>().LoadAllUserData();
            var users = ServiceManager.Get<UserService>().GetActiveUsers()
                .OrderBy(u => u.ID)
                .Skip(skip)
                .Take(pageSize);
            var result = new GetListOfUsersResponse();
            result.TotalCount = ServiceManager.Get<UserService>().GetActiveUserCount();
            foreach (var user in users)
            {
                result.Users.Add(UserMapper.ToUser(user.Model));
            }
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<ActionResult<GetSingleUserResponse>> AddUser([FromBody] NewUser newUser)
        {
            if (!Enum.TryParse<StreamingPlatformTypeEnum>(newUser.Platform, ignoreCase: true, out var platformEnum))
            {
                return BadRequest($"Unknown platform: {newUser.Platform}");
            }
            UserV2ViewModel user = await ServiceManager.Get<UserService>().GetUserByPlatform(platformEnum, platformUsername: newUser.Username, performPlatformSearch: true);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(new GetSingleUserResponse { User = UserMapper.ToUser(user.Model) });
        }

        [HttpDelete("{userId:guid}")]
        public async Task<IActionResult> DeleteUserById(Guid userId)
        {
            await ServiceManager.Get<UserService>().LoadAllUserData();
            if (!ChannelSession.Settings.Users.TryGetValue(userId, out var user) || user == null)
            {
                return NotFound();
            }
            ServiceManager.Get<UserService>().DeleteUserData(user.ID);
            return Ok();
        }
    }

    internal static class UserMapper
    {
        public static User ToUser(UserV2Model user) => new()
        {
            ID = user.ID,
            LastActivity = user.LastActivity,
            LastUpdated = user.LastUpdated,
            OnlineViewingMinutes = user.OnlineViewingMinutes,
            CurrencyAmounts = new(user.CurrencyAmounts),
            InventoryAmounts = new(user.InventoryAmounts),
            StreamPassAmounts = new(user.StreamPassAmounts),
            CustomTitle = user.CustomTitle,
            IsSpecialtyExcluded = user.IsSpecialtyExcluded,
            Notes = user.Notes,
            PlatformData = user.PlatformData.ToDictionary(k => k.Key.ToString(), v => ToUserPlatformData(v.Value)),
        };

        private static UserPlatformData ToUserPlatformData(UserPlatformV2ModelBase value) => new()
        {
            Platform = value.Platform.ToString(),
            ID = value.ID,
            Username = value.Username,
            DisplayName = value.DisplayName,
            AvatarLink = value.AvatarLink,
            SubscriberBadgeLink = value.SubscriberBadgeLink,
            RoleBadgeLink = value.RoleBadgeLink,
            SpecialtyBadgeLink = value.SpecialtyBadgeLink,
            Roles = new(value.Roles.Select(r => r.ToString())),
            AccountDate = value.AccountDate,
            FollowDate = value.FollowDate,
            SubscribeDate = value.SubscribeDate,
            SubscriberTier = value.SubscriberTier,
        };
    }
}
