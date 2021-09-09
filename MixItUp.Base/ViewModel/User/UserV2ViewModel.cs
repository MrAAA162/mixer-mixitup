﻿using MixItUp.Base.Model;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.User;
using MixItUp.Base.Model.User.Platform;
using MixItUp.Base.Services;
using MixItUp.Base.Services.External;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModels;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.User
{
    public class UserV2ViewModel : UIViewModelBase, IEquatable<UserV2ViewModel>, IComparable<UserV2ViewModel>
    {
        private const string UserDefaultColor = "MaterialDesignBody";

        private StreamingPlatformTypeEnum platform;
        private UserV2Model model;
        private UserPlatformV2ModelBase platformModel;

        private object cachePropertiesLock = new object();

        public UserV2ViewModel(StreamingPlatformTypeEnum platform, UserV2Model model)
        {
            this.platform = platform;
            this.model = model;
            
            if (this.platform != StreamingPlatformTypeEnum.None)
            {
                this.platformModel = this.Model.GetPlatformData<UserPlatformV2ModelBase>(this.platform);
            }
            else if (this.Model.HasPlatformData(ChannelSession.Settings.DefaultStreamingPlatform))
            {
                this.platformModel = this.Model.GetPlatformData<UserPlatformV2ModelBase>(ChannelSession.Settings.DefaultStreamingPlatform);
            }
            else
            {
                this.platformModel = this.Model.GetPlatformData<UserPlatformV2ModelBase>(this.Model.GetPlatforms().First());
            }
        }

        public UserV2Model Model { get { return this.Model; } }

        public UserPlatformV2ModelBase PlatformModel { get { return this.platformModel; } }

        public Guid ID { get { return this.Model.ID; } }

        public StreamingPlatformTypeEnum Platform { get { return this.platform; } }

        public HashSet<StreamingPlatformTypeEnum> AllPlatforms { get { return this.Model.GetPlatforms(); } }

        public bool IsUnassociated { get { return this.Platform == StreamingPlatformTypeEnum.None; } }

        public string PlatformID { get { return this.PlatformModel.ID; } }

        public string Username { get { return this.PlatformModel.Username; } }

        public string DisplayName { get { return !string.IsNullOrEmpty(this.PlatformModel.DisplayName) ? this.PlatformModel.DisplayName : this.Username; } }

        public string FullDisplayName
        {
            get
            {
                if (this.Platform == StreamingPlatformTypeEnum.Twitch)
                {
                    if (!string.IsNullOrEmpty(this.PlatformModel.DisplayName) && !string.Equals(this.DisplayName, this.Username, StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{this.DisplayName} ({this.Username})";
                    }
                    else
                    {
                        return this.DisplayName;
                    }
                }
                else
                {
                    return this.DisplayName;
                }
            }
        }

        public string AvatarLink { get { return this.PlatformModel.AvatarLink; } }

        public HashSet<UserRoleEnum> Roles { get { return this.PlatformModel.Roles; } }

        public UserRoleEnum PrimaryRole
        {
            get
            {
                lock (cachePropertiesLock)
                {
#pragma warning disable CS0612 // Type or member is obsolete
                    if (this.primaryRole == UserRoleEnum.Banned)
#pragma warning restore CS0612 // Type or member is obsolete
                    {
                        this.primaryRole = this.Roles.Max();
                    }
                    return this.primaryRole;
                }
            }
        }
#pragma warning disable CS0612 // Type or member is obsolete
        private UserRoleEnum primaryRole = UserRoleEnum.Banned;
#pragma warning restore CS0612 // Type or member is obsolete

        public string PrimaryRoleString { get { return EnumLocalizationHelper.GetLocalizedName(this.PrimaryRole); } }

        public bool IsPlatformSubscriber { get { return this.Roles.Contains(UserRoleEnum.Subscriber) || this.Roles.Contains(UserRoleEnum.YouTubeMember); } }

        public bool IsExternalSubscriber
        {
            get
            {
                if (this.PatreonUser != null && ServiceManager.Get<PatreonService>().IsConnected && !string.IsNullOrEmpty(ChannelSession.Settings.PatreonTierSubscriberEquivalent))
                {
                    PatreonTier userTier = this.PatreonTier;
                    PatreonTier equivalentTier = ServiceManager.Get<PatreonService>().Campaign.GetTier(ChannelSession.Settings.PatreonTierSubscriberEquivalent);
                    if (userTier != null && equivalentTier != null && userTier.Amount >= equivalentTier.Amount)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsSubscriber { get { return this.IsPlatformSubscriber || this.IsExternalSubscriber; } }

        public bool HasRole(UserRoleEnum role) { return this.Roles.Contains(role); }

        public bool MeetsRole(UserRoleEnum role)
        {
            if ((role == UserRoleEnum.Subscriber || role == UserRoleEnum.YouTubeMember) && this.IsSubscriber)
            {
                return true;
            }

            if (ChannelSession.Settings.ExplicitUserRoleRequirements)
            {
                return this.HasRole(role);
            }
            
            return this.PrimaryRole >= role;
        }

        public string Color
        {
            get
            {
                lock (cachePropertiesLock)
                {
                    if (this.color == null)
                    {
                        if (ChannelSession.Settings.UseCustomUsernameColors)
                        {
                            foreach (UserRoleEnum role in this.Roles.OrderByDescending(r => r))
                            {
                                if (ChannelSession.Settings.CustomUsernameColors.ContainsKey(role))
                                {
                                    string name = ChannelSession.Settings.CustomUsernameColors[role];
                                    if (ColorSchemes.HTMLColorSchemeDictionary.ContainsKey(name))
                                    {
                                        this.color = ColorSchemes.HTMLColorSchemeDictionary[name];
                                        break;
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(this.color))
                        {
                            if (this.Platform == StreamingPlatformTypeEnum.Twitch)
                            {
                                this.color = ((TwitchUserPlatformV2Model)this.PlatformModel).Color;
                            }
                        }

                        if (string.IsNullOrEmpty(this.color))
                        {
                            this.color = UserV2ViewModel.UserDefaultColor;
                        }
                    }
                    return this.color;
                }
            }
        }
        private string color;

        public string ChannelLink
        {
            get
            {
                if (this.Platform == StreamingPlatformTypeEnum.Twitch) { return $"https://www.twitch.tv/{this.Username}"; }
                else if (this.Platform == StreamingPlatformTypeEnum.YouTube) { return ((YouTubeUserPlatformV2Model)this.PlatformModel).YouTubeURL; }
                else if (this.Platform == StreamingPlatformTypeEnum.Glimesh) { return $"https://www.glimesh.tv/{this.Username}"; }
                else if (this.Platform == StreamingPlatformTypeEnum.Trovo) { return $"https://trovo.live/{this.Username}"; }
                return string.Empty;
            }
        }

        public DateTimeOffset? AccountDate { get { return this.PlatformModel.AccountDate; } set { this.PlatformModel.AccountDate = value; } }
        public string AccountDateString { get { return (this.AccountDate != null) ? this.AccountDate.GetValueOrDefault().GetAge() : MixItUp.Base.Resources.Unknown; } }

        public DateTimeOffset? FollowDate { get { return this.PlatformModel.FollowDate; } set { this.PlatformModel.FollowDate = value; } }
        public string FollowAgeString { get { return (this.FollowDate != null) ? this.FollowDate.GetValueOrDefault().GetAge() : MixItUp.Base.Resources.NotFollowing; } }
        public int FollowMonths { get { return (this.FollowDate != null) ? this.FollowDate.GetValueOrDefault().TotalMonthsFromNow() : 0; } }

        public DateTimeOffset? SubscribeDate { get { return this.PlatformModel.SubscribeDate; } set { this.PlatformModel.SubscribeDate = value; } }
        public string SubscribeAgeString { get { return (this.SubscribeDate != null) ? this.SubscribeDate.GetValueOrDefault().GetAge() : MixItUp.Base.Resources.NotSubscribed; } }
        public int SubscribeMonths { get { return (this.SubscribeDate != null) ? this.SubscribeDate.GetValueOrDefault().TotalMonthsFromNow() : 0; } }

        public int SubscribeTier { get { return this.PlatformModel.SubscriberTier; } }

        public int OnlineViewingMinutes
        {
            get { return this.PlatformModel.OnlineViewingMinutes; }
            set
            {
                this.PlatformModel.OnlineViewingMinutes = value;
                this.NotifyPropertyChanged("OnlineViewingMinutes");
                this.NotifyPropertyChanged("OnlineViewingMinutesOnly");
                this.NotifyPropertyChanged("OnlineViewingHoursOnly");
            }
        }

        public int OnlineViewingMinutesOnly
        {
            get { return this.OnlineViewingMinutes % 60; }
            set
            {
                this.OnlineViewingMinutes = (this.OnlineViewingHoursOnly * 60) + value;
                this.NotifyPropertyChanged("OnlineViewingMinutes");
                this.NotifyPropertyChanged("OnlineViewingMinutesOnly");
                this.NotifyPropertyChanged("OnlineViewingHoursOnly");
            }
        }

        public int OnlineViewingHoursOnly
        {
            get { return this.OnlineViewingMinutes / 60; }
            set
            {
                this.OnlineViewingMinutes = value * 60 + this.OnlineViewingMinutesOnly;
                this.NotifyPropertyChanged("OnlineViewingMinutes");
                this.NotifyPropertyChanged("OnlineViewingMinutesOnly");
                this.NotifyPropertyChanged("OnlineViewingHoursOnly");
            }
        }

        public long TotalStreamsWatched
        {
            get { return this.PlatformModel.TotalStreamsWatched; }
            set { this.PlatformModel.TotalStreamsWatched = value; }
        }

        public double TotalAmountDonated
        {
            get { return this.PlatformModel.TotalAmountDonated; }
            set { this.PlatformModel.TotalAmountDonated = value; }
        }

        public long TotalSubsGifted
        {
            get { return this.PlatformModel.TotalSubsGifted; }
            set { this.PlatformModel.TotalSubsGifted = value; }
        }

        public long TotalSubsReceived
        {
            get { return this.PlatformModel.TotalSubsReceived; }
            set { this.PlatformModel.TotalSubsReceived = value; }
        }

        public long TotalChatMessageSent
        {
            get { return this.PlatformModel.TotalChatMessageSent; }
            set { this.PlatformModel.TotalChatMessageSent = value; }
        }

        public long TotalTimesTagged
        {
            get { return this.PlatformModel.TotalTimesTagged; }
            set { this.PlatformModel.TotalTimesTagged = value; }
        }

        public long TotalCommandsRun
        {
            get { return this.PlatformModel.TotalCommandsRun; }
            set { this.PlatformModel.TotalCommandsRun = value; }
        }

        public long TotalMonthsSubbed
        {
            get { return this.PlatformModel.TotalMonthsSubbed; }
            set { this.PlatformModel.TotalMonthsSubbed = value; }
        }

        public uint ModerationStrikes
        {
            get { return this.Model.ModerationStrikes; }
            set { this.Model.ModerationStrikes = value; }
        }

        public bool IsSpecialtyExcluded
        {
            get { return this.Model.IsSpecialtyExcluded; }
            set { this.Model.IsSpecialtyExcluded = value; }
        }

        public CommandModelBase EntranceCommand
        {
            get { return ChannelSession.Settings.GetCommand(this.Model.EntranceCommandID); }
            set { this.Model.EntranceCommandID = (value != null) ? value.ID : Guid.Empty; }
        }

        public DateTimeOffset LastActivity { get { return this.Model.LastActivity; } }

        public bool UpdatedThisSession { get; set; }

        public bool IsInChat { get; set; }

        public string SortableID
        {
            get
            {
                lock (cachePropertiesLock)
                {
                    if (this.sortableID == null)
                    {
                        UserRoleEnum role = this.PrimaryRole;
                        if (role < UserRoleEnum.Subscriber)
                        {
                            role = UserRoleEnum.User;
                        }
                        this.sortableID = (999 - role) + "-" + this.Username + "-" + this.Platform.ToString();
                    }
                    return this.sortableID;
                }
            }
        }
        private string sortableID;

        public int WhispererNumber { get; set; }

        public bool HasWhisperNumber { get { return this.WhispererNumber > 0; } }

        public PatreonCampaignMember PatreonUser { get; private set; }

        public PatreonTier PatreonTier
        {
            get
            {
                if (ServiceManager.Get<PatreonService>().IsConnected && this.PatreonUser != null)
                {
                    return ServiceManager.Get<PatreonService>().Campaign.GetTier(this.PatreonUser.TierID);
                }
                return null;
            }
        }

        public void UpdateLastActivity() { this.Model.LastActivity = DateTimeOffset.Now; }

        public void UpdateViewingMinutes()
        {
            this.OnlineViewingMinutes++;
            ChannelSession.Settings.Users.ManualValueChanged(this.ID);

            if (ChannelSession.Settings.RegularUserMinimumHours > 0 && this.OnlineViewingHoursOnly >= ChannelSession.Settings.RegularUserMinimumHours)
            {
                this.Roles.Add(UserRoleEnum.Regular);
            }
            else
            {
                this.Roles.Remove(UserRoleEnum.Regular);
            }
        }

        public async Task AddModerationStrike(string moderationReason = null)
        {
            Dictionary<string, string> extraSpecialIdentifiers = new Dictionary<string, string>();
            extraSpecialIdentifiers.Add(ModerationService.ModerationReasonSpecialIdentifier, moderationReason);

            this.ModerationStrikes++;
            if (this.ModerationStrikes == 1)
            {
                // TODO
                //await ServiceManager.Get<CommandService>().Queue(ChannelSession.Settings.ModerationStrike1CommandID, new CommandParametersModel(this, extraSpecialIdentifiers));
            }
            else if (this.ModerationStrikes == 2)
            {
                // TODO
                //await ServiceManager.Get<CommandService>().Queue(ChannelSession.Settings.ModerationStrike2CommandID, new CommandParametersModel(this, extraSpecialIdentifiers));
            }
            else if (this.ModerationStrikes >= 3)
            {
                // TODO
                //await ServiceManager.Get<CommandService>().Queue(ChannelSession.Settings.ModerationStrike3CommandID, new CommandParametersModel(this, extraSpecialIdentifiers));
            }
        }

        public void RemoveModerationStrike()
        {
            if (this.ModerationStrikes > 0)
            {
                this.ModerationStrikes--;
            }
        }

        public async Task Refresh(bool force = false)
        {
            if (!this.IsUnassociated)
            {
                if (!this.UpdatedThisSession || force)
                {
                    this.UpdatedThisSession = true;
                    this.UpdateLastActivity();

                    DateTimeOffset refreshStart = DateTimeOffset.Now;

                    await this.platformModel.Refresh();

                    this.RefreshPatreonProperties();

                    this.ClearCachedProperties();

                    double refreshTime = (DateTimeOffset.Now - refreshStart).TotalMilliseconds;
                    Logger.Log($"User refresh time: {refreshTime} ms");
                    if (refreshTime > 500)
                    {
                        Logger.Log(LogLevel.Error, string.Format("Long user refresh time detected for the following user: {0} - {1} - {2} ms", this.ID, this.Username, refreshTime));
                    }
                }
            }
        }

        public void RefreshPatreonProperties()
        {
            if (ServiceManager.Get<PatreonService>().IsConnected && this.PatreonUser == null)
            {
                IEnumerable<PatreonCampaignMember> campaignMembers = ServiceManager.Get<PatreonService>().CampaignMembers;

                if (!string.IsNullOrEmpty(this.model.PatreonUserID))
                {
                    this.PatreonUser = campaignMembers.FirstOrDefault(u => u.UserID.Equals(this.model.PatreonUserID));
                }
                else
                {
                    this.PatreonUser = campaignMembers.FirstOrDefault(u => this.Platform.HasFlag(u.User.Platform) && string.Equals(u.User.PlatformUserID, this.PlatformID, StringComparison.InvariantCultureIgnoreCase));
                }

                if (this.PatreonUser != null)
                {
                    this.model.PatreonUserID = this.PatreonUser.UserID;
                }
                else
                {
                    this.model.PatreonUserID = null;
                }
            }
        }

        private void ClearCachedProperties()
        {
            lock (cachePropertiesLock)
            {
                this.color = null;
                this.sortableID = null;
#pragma warning disable CS0612 // Type or member is obsolete
                this.primaryRole = UserRoleEnum.Banned;
#pragma warning restore CS0612 // Type or member is obsolete
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is UserV2ViewModel)
            {
                return this.Equals((UserV2ViewModel)obj);
            }
            return false;
        }

        public bool Equals(UserV2ViewModel other)
        {
            return this.ID.Equals(other.ID);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        public int CompareTo(UserV2ViewModel other) { return this.Username.CompareTo(other.Username); }

        public override string ToString() { return this.Username; }
    }
}
