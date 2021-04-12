﻿using MixItUp.Base.Model;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.Model.User;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.User;
using MixItUp.SignalR.Client;
using StreamingClient.Base.Model.OAuth;
using StreamingClient.Base.Services;
using StreamingClient.Base.Util;
using System;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public interface IWebhookService
    {
        Task Authenticate(string twitchAccessToken);
    }

    public class WebhookService : OAuthRestServiceBase, IWebhookService
    {
        public const string AuthenticateMethodName = "Authenticate";

        private readonly string apiAddress;
        private readonly SignalRConnection signalRConnection;

        public bool IsConnected { get { return this.signalRConnection.IsConnected(); } }
        public bool IsAllowed { get; private set; } = false;


        public WebhookService(string apiAddress, string webhookHubAddress)
        {
            this.apiAddress = apiAddress;
            this.signalRConnection = new SignalRConnection(webhookHubAddress);
            this.signalRConnection.Connected += SignalRConnection_Connected;
            this.signalRConnection.Disconnected += SignalRConnection_Disconnected;

            this.signalRConnection.Listen("TwitchFollowEvent", (string followerId, string followerUsername, string followerDisplayName) =>
            {
                var _ = this.TwitchFollowEvent(followerId, followerUsername, followerDisplayName);
            });

            this.signalRConnection.Listen("TwitchStreamStartedEvent", () =>
            {
                var _ = this.TwitchStreamStartedEvent();
            });

            this.signalRConnection.Listen("AuthenticationCompleteEvent", (bool approved) =>
            {
                IsAllowed = approved;
                if (!IsAllowed)
                {
                    // Force disconnect is it doesn't retry
                    var _ = this.signalRConnection.Disconnect();
                }
            });
        }

        private void SignalRConnection_Disconnected(object sender, Exception e)
        {
        }

        private async void SignalRConnection_Connected(object sender, EventArgs e)
        {
            var twitchUserOAuthToken = ChannelSession.TwitchUserConnection.Connection.GetOAuthTokenCopy();
            await this.Authenticate(twitchUserOAuthToken?.accessToken);
        }

        public async Task Authenticate(string twitchAccessToken)
        {
            await this.AsyncWrapper(this.signalRConnection.Send(AuthenticateMethodName, twitchAccessToken));
        }

        protected override string GetBaseAddress() { return this.apiAddress; }

        protected override Task<OAuthTokenModel> GetOAuthToken(bool autoRefreshToken = true) { return Task.FromResult<OAuthTokenModel>(new OAuthTokenModel()); }

        private async Task AsyncWrapper(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        public async Task Connect() { await this.signalRConnection.Connect(); }

        public async Task<bool> InitializeConnection()
        {
            if (!this.IsConnected)
            {
                await this.Connect();

                return this.IsConnected;
            }
            return true;
        }

        private async Task TwitchFollowEvent(string followerId, string followerUsername, string followerDisplayName)
        {
            UserViewModel user = ChannelSession.Services.User.GetUserByTwitchID(followerId);
            if (user == null)
            {
                user = new UserViewModel(followerDisplayName);
            }

            if (user.UserRoles.Contains(UserRoleEnum.Banned))
            {
                return;
            }

            EventTrigger trigger = new EventTrigger(EventTypeEnum.TwitchChannelFollowed, user);
            if (ChannelSession.Services.Events.CanPerformEvent(trigger))
            {
                user.FollowDate = DateTimeOffset.Now;

                ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestFollowerUserData] = user.ID;

                foreach (CurrencyModel currency in ChannelSession.Settings.Currency.Values)
                {
                    currency.AddAmount(user.Data, currency.OnFollowBonus);
                }

                foreach (StreamPassModel streamPass in ChannelSession.Settings.StreamPass.Values)
                {
                    if (user.HasPermissionsTo(streamPass.Permission))
                    {
                        streamPass.AddAmount(user.Data, streamPass.FollowBonus);
                    }
                }

                await ChannelSession.Services.Events.PerformEvent(trigger);

                GlobalEvents.FollowOccurred(user);

                await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Twitch, user, string.Format("{0} Followed", user.DisplayName), ChannelSession.Settings.AlertFollowColor));
            }
        }

        private async Task TwitchStreamStartedEvent()
        {
            EventTrigger trigger = new EventTrigger(EventTypeEnum.TwitchChannelStreamStart, ChannelSession.GetCurrentUser());
            if (ChannelSession.Services.Events.CanPerformEvent(trigger))
            {
                await ChannelSession.Services.Events.PerformEvent(trigger);
            }
        }
    }
}
