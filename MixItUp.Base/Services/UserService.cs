﻿using MixItUp.Base.Model;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.User;
using MixItUp.Base.Model.User.Platform;
using MixItUp.Base.Services.Glimesh;
using MixItUp.Base.Services.Trovo;
using MixItUp.Base.Services.Twitch;
using MixItUp.Base.Services.YouTube;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public class UserService
    {
        public static string SanitizeUsername(string username) { return !string.IsNullOrEmpty(username) ? username.ToLower().Replace("@", "").Trim() : string.Empty; }

        private Dictionary<StreamingPlatformTypeEnum, LockedDictionary<string, Guid>> platformUserIDLookups { get; set; } = new Dictionary<StreamingPlatformTypeEnum, LockedDictionary<string, Guid>>();
        private Dictionary<StreamingPlatformTypeEnum, LockedDictionary<string, Guid>> platformUsernameLookups { get; set; } = new Dictionary<StreamingPlatformTypeEnum, LockedDictionary<string, Guid>>();

        private Dictionary<Guid, UserV2ViewModel> activeUsers = new Dictionary<Guid, UserV2ViewModel>();

        public int ActiveUserCount { get { return this.activeUsers.Count; } }

        public IEnumerable<UserV2ViewModel> DisplayUsers
        {
            get
            {
                lock (displayUsersLock)
                {
                    return this.displayUsers.Values.ToList().Take(ChannelSession.Settings.MaxUsersShownInChat);
                }
            }
        }
        private SortedList<string, UserV2ViewModel> displayUsers = new SortedList<string, UserV2ViewModel>();
        private object displayUsersLock = new object();

        public event EventHandler DisplayUsersUpdated = delegate { };

        private bool fullUserDataLoadOccurred = false;

        public UserService()
        {
            StreamingPlatforms.ForEachPlatform((p) =>
            {
                this.platformUserIDLookups[p] = new LockedDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
                this.platformUsernameLookups[p] = new LockedDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            });
        }

        #region Users

        public async Task<UserV2ViewModel> GetUserByID(Guid id)
        {
            if (this.activeUsers.TryGetValue(id, out UserV2ViewModel user))
            {
                return user;
            }

            if (ChannelSession.Settings.Users.ContainsKey(id))
            {
                return new UserV2ViewModel(ChannelSession.Settings.DefaultStreamingPlatform, ChannelSession.Settings.Users[id]);
            }

            IEnumerable<UserV2Model> results = await ChannelSession.Settings.LoadUserV2Data("SELECT * FROM Users WHERE ID = @ID", new Dictionary<string, object>() { { "ID", id } });
            if (results.Count() > 0)
            {
                this.SetUserData(results.First());
                return new UserV2ViewModel(ChannelSession.Settings.DefaultStreamingPlatform, results.First());
            }

            return null;
        }

        public async Task<UserV2ViewModel> GetUserByPlatformID(StreamingPlatformTypeEnum platform, string platformID, bool performPlatformSearch = false)
        {
            UserV2ViewModel user = null;

            if (string.IsNullOrEmpty(platformID))
            {
                return user;
            }

            if (platform == StreamingPlatformTypeEnum.None || platform == StreamingPlatformTypeEnum.All)
            {
                return user;
            }

            if (this.platformUserIDLookups.ContainsKey(platform) && this.platformUserIDLookups[platform].TryGetValue(platformID, out Guid id))
            {
                user = await this.GetUserByID(id);
                if (user != null)
                {
                    return user;
                }
            }

            string platformLookup = null;
            switch (platform)
            {
                case StreamingPlatformTypeEnum.Twitch: platformLookup = "TwitchID"; break;
                case StreamingPlatformTypeEnum.YouTube: platformLookup = "YouTubeID"; break;
                case StreamingPlatformTypeEnum.Trovo: platformLookup = "TrovoID"; break;
                case StreamingPlatformTypeEnum.Glimesh: platformLookup = "GlimeshID"; break;
            }

            if (!string.IsNullOrEmpty(platformLookup))
            {
                IEnumerable<UserV2Model> results = await ChannelSession.Settings.LoadUserV2Data($"SELECT * FROM Users WHERE {platformLookup} = @PlatformID", new Dictionary<string, object>() { { "PlatformID", platformID } });
                if (results.Count() > 0)
                {
                    this.SetUserData(results.First());
                    return new UserV2ViewModel(platform, results.First());
                }
            }

            if (performPlatformSearch)
            {
                UserPlatformV2ModelBase platformModel = null;
                if (platform == StreamingPlatformTypeEnum.Twitch && ServiceManager.Get<TwitchSessionService>().UserConnection != null)
                {
                    var twitchUser = await ServiceManager.Get<TwitchSessionService>().UserConnection.GetNewAPIUserByID(platformID);
                    if (twitchUser != null)
                    {
                        platformModel = new TwitchUserPlatformV2Model(twitchUser);
                    }
                }
                else if (platform == StreamingPlatformTypeEnum.YouTube && ServiceManager.Get<YouTubeSessionService>().UserConnection != null)
                {
                    var youtubeUser = await ServiceManager.Get<YouTubeSessionService>().UserConnection.GetChannelByID(platformID);
                    if (youtubeUser != null)
                    {
                        platformModel = new YouTubeUserPlatformV2Model(youtubeUser);
                    }
                }
                else if (platform == StreamingPlatformTypeEnum.Glimesh && ServiceManager.Get<GlimeshSessionService>().UserConnection != null)
                {
                    var glimeshUser = await ServiceManager.Get<GlimeshSessionService>().UserConnection.GetUserByID(platformID);
                    if (glimeshUser != null)
                    {
                        platformModel = new GlimeshUserPlatformV2Model(glimeshUser);
                    }
                }
                else if (platform == StreamingPlatformTypeEnum.Trovo && ServiceManager.Get<TrovoSessionService>().UserConnection != null)
                {
                    throw new InvalidOperationException("Trovo does not support user look-up by user ID");
                }

                return await this.CreateUser(platformModel);
            }

            return null;
        }

        public async Task<UserV2ViewModel> GetUserByPlatformUsername(StreamingPlatformTypeEnum platform, string platformUsername, bool performPlatformSearch = false)
        {
            UserV2ViewModel user = null;

            platformUsername = UserService.SanitizeUsername(platformUsername);
            if (string.IsNullOrEmpty(platformUsername))
            {
                return user;
            }

            if (platform == StreamingPlatformTypeEnum.None || platform == StreamingPlatformTypeEnum.All)
            {
                await StreamingPlatforms.ForEachPlatform(async (p) =>
                {
                    user = await this.GetUserByPlatformUsername(p, platformUsername, performPlatformSearch);
                    if (user != null)
                    {
                        return;
                    }
                });
                return user;
            }

            if (this.platformUsernameLookups.ContainsKey(platform) && this.platformUsernameLookups[platform].TryGetValue(platformUsername, out Guid id))
            {
                user = await this.GetUserByID(id);
                if (user != null)
                {
                    return user;
                }
            }

            string platformLookup = null;
            switch (platform)
            {
                case StreamingPlatformTypeEnum.Twitch: platformLookup = "TwitchUsername"; break;
                case StreamingPlatformTypeEnum.YouTube: platformLookup = "YouTubeUsername"; break;
                case StreamingPlatformTypeEnum.Trovo: platformLookup = "TrovoUsername"; break;
                case StreamingPlatformTypeEnum.Glimesh: platformLookup = "GlimeshUsername"; break;
            }

            if (!string.IsNullOrEmpty(platformLookup))
            {
                IEnumerable<UserV2Model> results = await ChannelSession.Settings.LoadUserV2Data($"SELECT * FROM Users WHERE {platformLookup} = @PlatformUsername", new Dictionary<string, object>() { { "PlatformUsername", platformUsername } });
                if (results.Count() > 0)
                {
                    this.SetUserData(results.First());
                    return new UserV2ViewModel(platform, results.First());
                }
            }

            if (performPlatformSearch)
            {
                UserPlatformV2ModelBase platformModel = null;
                if (platform == StreamingPlatformTypeEnum.Twitch && ServiceManager.Get<TwitchSessionService>().UserConnection != null)
                {
                    var twitchUser = await ServiceManager.Get<TwitchSessionService>().UserConnection.GetNewAPIUserByLogin(platformUsername);
                    if (twitchUser != null)
                    {
                        platformModel = new TwitchUserPlatformV2Model(twitchUser);
                    }
                }
                else if (platform == StreamingPlatformTypeEnum.YouTube && ServiceManager.Get<YouTubeSessionService>().UserConnection != null)
                {
                    var youtubeUser = await ServiceManager.Get<YouTubeSessionService>().UserConnection.GetChannelByUsername(platformUsername);
                    if (youtubeUser != null)
                    {
                        platformModel = new YouTubeUserPlatformV2Model(youtubeUser);
                    }
                }
                else if (platform == StreamingPlatformTypeEnum.Glimesh && ServiceManager.Get<GlimeshSessionService>().UserConnection != null)
                {
                    var glimeshUser = await ServiceManager.Get<GlimeshSessionService>().UserConnection.GetUserByName(platformUsername);
                    if (glimeshUser != null)
                    {
                        platformModel = new GlimeshUserPlatformV2Model(glimeshUser);
                    }
                }
                else if (platform == StreamingPlatformTypeEnum.Trovo && ServiceManager.Get<TrovoSessionService>().UserConnection != null)
                {
                    var trovoUser = await ServiceManager.Get<TrovoSessionService>().UserConnection.GetUserByName(platformUsername);
                    if (trovoUser != null)
                    {
                        platformModel = new TrovoUserPlatformV2Model(trovoUser);
                    }
                }

                return await this.CreateUser(platformModel);
            }

            return null;
        }

        public async Task<UserV2ViewModel> CreateUser(UserPlatformV2ModelBase platformModel)
        {
            if (platformModel != null && !string.IsNullOrEmpty(platformModel.ID))
            {
                UserV2ViewModel user = await this.GetUserByPlatformID(platformModel.Platform, platformModel.ID, performPlatformSearch: false);
                if (user == null)
                {
                    UserV2Model userModel = new UserV2Model();
                    userModel.AddPlatformData(platformModel);
                    user = new UserV2ViewModel(platformModel.Platform, userModel);

                    this.SetUserData(userModel, newData: true);
                }
                return user;
            }
            return null;
        }

        public static UserV2ViewModel CreateUserViewModel(UserV2Model user)
        {
            if (user != null)
            {
                return new UserV2ViewModel(ChannelSession.Settings.DefaultStreamingPlatform, user);
            }
            return null;
        }

        public async Task LoadAllUserData()
        {
            if (!this.fullUserDataLoadOccurred)
            {
                this.fullUserDataLoadOccurred = true;

                foreach (UserV2Model userData in await ChannelSession.Settings.LoadUserV2Data("SELECT * FROM Users", new Dictionary<string, object>()))
                {
                    this.SetUserData(userData);
                }
            }
        }

        public async Task ClearAllUserData()
        {
            this.platformUserIDLookups.Clear();
            this.platformUsernameLookups.Clear();
            this.activeUsers.Clear();

            await ChannelSession.Settings.ClearUserV2Data();
        }

        private void SetUserData(UserV2Model userData, bool newData = false)
        {
            if (userData != null && userData.ID != Guid.Empty && userData.GetPlatforms().Count() > 0 && !userData.HasPlatformData(StreamingPlatformTypeEnum.None))
            {
                lock (ChannelSession.Settings.Users)
                {
                    if (!ChannelSession.Settings.Users.ContainsKey(userData.ID))
                    {
                        ChannelSession.Settings.Users[userData.ID] = userData;
                        if (ChannelSession.Settings.ModerationResetStrikesOnLaunch)
                        {
                            userData.ModerationStrikes = 0;
                        }

                        ChannelSession.Settings.Users.ManualValueChanged(userData.ID);
                    }

                    foreach (StreamingPlatformTypeEnum platform in userData.GetPlatforms())
                    {
                        UserPlatformV2ModelBase platformModel = userData.GetPlatformData<UserPlatformV2ModelBase>(platform);
                        this.platformUserIDLookups[platform][platformModel.ID] = userData.ID;
                        this.platformUsernameLookups[platform][platformModel.Username] = userData.ID;
                    }
                }
            }
        }

        #endregion Users

        #region Active Users

        public UserV2ViewModel GetActiveUserByID(Guid id)
        {
            if (this.activeUsers.TryGetValue(id, out UserV2ViewModel user))
            {
                return user;
            }
            return null;
        }

        public UserV2ViewModel GetActiveUserByPlatformID(StreamingPlatformTypeEnum platform, string platformID)
        {
            UserV2ViewModel user = null;

            if (string.IsNullOrEmpty(platformID))
            {
                return user;
            }

            if (platform == StreamingPlatformTypeEnum.None || platform == StreamingPlatformTypeEnum.All)
            {
                return user;
            }

            if (this.platformUserIDLookups.ContainsKey(platform) && this.platformUserIDLookups[platform].TryGetValue(platformID, out Guid id))
            {
                user = this.GetActiveUserByID(id);
                if (user != null)
                {
                    return user;
                }
            }

            return null;
        }

        public UserV2ViewModel GetActiveUserByPlatformUsername(StreamingPlatformTypeEnum platform, string platformUsername)
        {
            UserV2ViewModel user = null;

            platformUsername = UserService.SanitizeUsername(platformUsername);
            if (string.IsNullOrEmpty(platformUsername))
            {
                return user;
            }

            if (platform == StreamingPlatformTypeEnum.None || platform == StreamingPlatformTypeEnum.All)
            {
                StreamingPlatforms.ForEachPlatform((p) =>
                {
                    user = this.GetActiveUserByPlatformUsername(p, platformUsername);
                    if (user != null)
                    {
                        return;
                    }
                });
                return user;
            }

            if (this.platformUsernameLookups.ContainsKey(platform) && this.platformUsernameLookups[platform].TryGetValue(platformUsername, out Guid id))
            {
                user = this.GetActiveUserByID(id);
                if (user != null)
                {
                    return user;
                }
            }

            return null;
        }

        public bool IsUserActive(Guid userID) { return this.GetActiveUserByID(userID) != null; }

        public async Task AddOrUpdateActiveUser(IEnumerable<UserV2ViewModel> users)
        {
            List<AlertChatMessageViewModel> alerts = new List<AlertChatMessageViewModel>();

            bool usersAdded = false;
            foreach (UserV2ViewModel user in users)
            {
                if (user == null || user.ID == Guid.Empty)
                {
                    return;
                }

                lock (this.activeUsers)
                {
                    if (this.activeUsers.ContainsKey(user.ID))
                    {
                        continue;
                    }
                    this.activeUsers[user.ID] = user;
                }

                usersAdded = true;
                this.SetUserData(user.Model);

                lock (displayUsersLock)
                {
                    this.displayUsers[user.SortableID] = user;
                }

                if (user.OnlineViewingMinutes == 0)
                {
                    await ServiceManager.Get<EventService>().PerformEvent(EventTypeEnum.ChatUserFirstJoin, new CommandParametersModel(user));
                }

                CommandParametersModel parameters = new CommandParametersModel(user);
                if (ServiceManager.Get<EventService>().CanPerformEvent(EventTypeEnum.ChatUserJoined, parameters))
                {
                    user.UpdateLastActivity();
                    user.Model.TotalStreamsWatched++;
                    await ServiceManager.Get<EventService>().PerformEvent(EventTypeEnum.ChatUserJoined, parameters);

                    alerts.Add(new AlertChatMessageViewModel(user, string.Format(MixItUp.Base.Resources.UserJoinedChat, user.FullDisplayName), ChannelSession.Settings.AlertUserJoinLeaveColor));
                }
            }

            if (usersAdded)
            {
                this.DisplayUsersUpdated(this, new EventArgs());

                if (alerts.Count() < 5)
                {
                    foreach (AlertChatMessageViewModel alert in alerts)
                    {
                        await ServiceManager.Get<AlertsService>().AddAlert(alert);
                    }
                }
            }
        }

        public async Task AddOrUpdateActiveUser(UserV2ViewModel user)
        {
            if (user == null || user.ID == Guid.Empty)
            {
                return;
            }
            await this.AddOrUpdateActiveUser(new List<UserV2ViewModel>() { user });
        }

        public async Task<UserV2ViewModel> RemoveActiveUser(StreamingPlatformTypeEnum platform, string platformUsername)
        {
            if (this.platformUsernameLookups.ContainsKey(platform) && this.platformUsernameLookups[platform].TryGetValue(platformUsername, out Guid id))
            {
                return await this.RemoveActiveUser(id);
            }
            return null;
        }

        public async Task<UserV2ViewModel> RemoveActiveUser(Guid id)
        {
            if (this.activeUsers.TryGetValue(id, out UserV2ViewModel user))
            {
                await this.RemoveActiveUser(user);
                return user;
            }
            return null;
        }

        public async Task<UserV2ViewModel> RemoveActiveUser(UserV2ViewModel user)
        {
            if (user != null && this.activeUsers.ContainsKey(user.ID))
            {
                await this.RemoveActiveUsers(new List<UserV2ViewModel>() { user });
            }
            return user;
        }

        public async Task RemoveActiveUsers(IEnumerable<UserV2ViewModel> users)
        {
            List<AlertChatMessageViewModel> alerts = new List<AlertChatMessageViewModel>();

            bool userRemoved = false;
            foreach (UserV2ViewModel user in users)
            {
                if (this.activeUsers.Remove(user.ID))
                {
                    userRemoved = true;

                    lock (displayUsersLock)
                    {
                        if (!this.displayUsers.Remove(user.SortableID))
                        {
                            int index = this.displayUsers.IndexOfValue(user);
                            if (index >= 0)
                            {
                                this.displayUsers.RemoveAt(index);
                            }
                        }
                    }

                    CommandParametersModel parameters = new CommandParametersModel(user);
                    if (ServiceManager.Get<EventService>().CanPerformEvent(EventTypeEnum.ChatUserLeft, parameters))
                    {
                        await ServiceManager.Get<EventService>().PerformEvent(EventTypeEnum.ChatUserLeft, parameters);

                        alerts.Add(new AlertChatMessageViewModel(user, string.Format(MixItUp.Base.Resources.UserLeftChat, user.FullDisplayName), ChannelSession.Settings.AlertUserJoinLeaveColor));
                    }
                }
            }

            if (userRemoved)
            {
                this.DisplayUsersUpdated(this, new EventArgs());

                if (users.Count() < 5)
                {
                    foreach (AlertChatMessageViewModel alert in alerts)
                    {
                        await ServiceManager.Get<AlertsService>().AddAlert(alert);
                    }
                }
            }
        }

        public IEnumerable<UserV2ViewModel> GetActiveUsers(StreamingPlatformTypeEnum platform = StreamingPlatformTypeEnum.All)
        {
            if (platform == StreamingPlatformTypeEnum.None || platform == StreamingPlatformTypeEnum.All)
            {
                return this.activeUsers.Values.ToList();
            }
            else
            {
                return this.activeUsers.Values.ToList().Where(u => u.Platform == platform);
            }
        }

        public int GetActiveUserCount() { return this.activeUsers.Count; }

        public UserV2ViewModel GetRandomActiveUser(StreamingPlatformTypeEnum platform = StreamingPlatformTypeEnum.All, bool excludeSpecialtyExcluded = true)
        {
            IEnumerable<UserV2ViewModel> users = this.GetActiveUsers(platform);
            if (excludeSpecialtyExcluded)
            {
                users = users.Where(u => !u.IsSpecialtyExcluded);
            }
            return users.Random();
        }

        #endregion Active Users
    }
}
