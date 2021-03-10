﻿using MixItUp.Base.Services;
using MixItUp.Base.Services.Glimesh;
using MixItUp.Base.Services.Trovo;
using MixItUp.Base.Services.Twitch;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Commands
{
    [DataContract]
    public class CommandParametersModel
    {
        public static async Task<UserViewModel> SearchForUser(string username, StreamingPlatformTypeEnum platform = StreamingPlatformTypeEnum.All)
        {
            username = username.Replace("@", "");
            UserViewModel user = ServiceManager.Get<UserService>().GetUserByUsername(username, platform);
            if (user == null)
            {
                if (platform.HasFlag(StreamingPlatformTypeEnum.Twitch) && ServiceManager.Get<TwitchSessionService>().UserConnection != null)
                {
                    Twitch.Base.Models.NewAPI.Users.UserModel twitchUser = await ServiceManager.Get<TwitchSessionService>().UserConnection.GetNewAPIUserByLogin(username);
                    if (twitchUser != null)
                    {
                        return new UserViewModel(twitchUser);
                    }
                }

                if (platform.HasFlag(StreamingPlatformTypeEnum.Glimesh) && ServiceManager.Get<GlimeshSessionService>().UserConnection != null)
                {
                    Glimesh.Base.Models.Users.UserModel glimeshUser = await ServiceManager.Get<GlimeshSessionService>().UserConnection.GetUserByName(username);
                    if (glimeshUser != null)
                    {
                        return new UserViewModel(glimeshUser);
                    }
                }

                if (platform.HasFlag(StreamingPlatformTypeEnum.Trovo) && ServiceManager.Get<TrovoSessionService>().UserConnection != null)
                {
                    Trovo.Base.Models.Users.UserModel trovoUser = await ServiceManager.Get<TrovoSessionService>().UserConnection.GetUserByName(username);
                    if (trovoUser != null)
                    {
                        return new UserViewModel(trovoUser);
                    }
                }
            }
            return user;
        }

        public static CommandParametersModel GetTestParameters(Dictionary<string, string> specialIdentifiers)
        {
            return new CommandParametersModel(ChannelSession.GetCurrentUser(), StreamingPlatformTypeEnum.All, new List<string>() { "@" + ChannelSession.GetCurrentUser().Username }, specialIdentifiers) { TargetUser = ChannelSession.GetCurrentUser() };
        }

        [DataMember]
        public UserViewModel User { get; set; } = ChannelSession.GetCurrentUser();
        [DataMember]
        public StreamingPlatformTypeEnum Platform { get; set; } = StreamingPlatformTypeEnum.All;
        [DataMember]
        public List<string> Arguments { get; set; } = new List<string>();
        [DataMember]
        public Dictionary<string, string> SpecialIdentifiers { get; set; } = new Dictionary<string, string>();

        [DataMember]
        public UserViewModel TargetUser { get; set; }

        [DataMember]
        public bool WaitForCommandToFinish { get; set; }
        [DataMember]
        public bool DontLockCommand { get; set; }

        public CommandParametersModel() : this(ChannelSession.GetCurrentUser()) { }

        public CommandParametersModel(UserViewModel user) : this(user, StreamingPlatformTypeEnum.None) { }

        public CommandParametersModel(Dictionary<string, string> specialIdentifiers) : this(ChannelSession.GetCurrentUser(), specialIdentifiers) { }

        public CommandParametersModel(UserViewModel user, StreamingPlatformTypeEnum platform) : this(user, platform, null) { }

        public CommandParametersModel(UserViewModel user, IEnumerable<string> arguments) : this(user, StreamingPlatformTypeEnum.None, arguments, null) { }

        public CommandParametersModel(UserViewModel user, Dictionary<string, string> specialIdentifiers) : this(user, null, specialIdentifiers) { }

        public CommandParametersModel(UserViewModel user, StreamingPlatformTypeEnum platform, IEnumerable<string> arguments) : this(user, platform, arguments, null) { }

        public CommandParametersModel(UserViewModel user, IEnumerable<string> arguments, Dictionary<string, string> specialIdentifiers) : this(user, StreamingPlatformTypeEnum.None, arguments, specialIdentifiers) { }

        public CommandParametersModel(ChatMessageViewModel message) : this(message.User, message.Platform, message.ToArguments()) { }

        public CommandParametersModel(UserViewModel user = null, StreamingPlatformTypeEnum platform = StreamingPlatformTypeEnum.All, IEnumerable<string> arguments = null, Dictionary<string, string> specialIdentifiers = null)
        {
            if (user != null)
            {
                this.User = user;
            }

            if (arguments != null)
            {
                this.Arguments = new List<string>(arguments);
            }

            if (specialIdentifiers != null)
            {
                this.SpecialIdentifiers = new Dictionary<string, string>(specialIdentifiers);
            }

            if (platform != StreamingPlatformTypeEnum.None)
            {
                this.Platform = platform;
            }
            else
            {
                this.Platform = this.User.Platform;
            }
        }

        public bool IsTargetUserSelf { get { return this.TargetUser == this.User; } }

        public CommandParametersModel Duplicate()
        {
            CommandParametersModel result = new CommandParametersModel(this.User, this.Platform, this.Arguments, this.SpecialIdentifiers);
            result.TargetUser = this.TargetUser;
            result.WaitForCommandToFinish = this.WaitForCommandToFinish;
            result.DontLockCommand = this.DontLockCommand;
            return result;
        }

        public async Task SetTargetUser()
        {
            if (this.TargetUser == null)
            {
                if (this.Arguments.Count > 0)
                {
                    this.TargetUser = await CommandParametersModel.SearchForUser(this.Arguments.First(), this.Platform);
                }

                if (this.TargetUser == null || !this.Arguments.ElementAt(0).Replace("@", "").Equals(this.TargetUser.Username, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.TargetUser = this.User;
                }
            }
        }
    }
}
