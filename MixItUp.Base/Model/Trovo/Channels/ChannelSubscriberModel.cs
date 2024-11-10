﻿using MixItUp.Base.Model.Trovo.Users;

namespace MixItUp.Base.Model.Trovo.Channels
{
    /// <summary>
    /// Information about a channel subscriber.
    /// </summary>
    public class ChannelSubscriberModel
    {
        /// <summary>
        /// The user information.
        /// </summary>
        public UserModel user { get; set; }

        /// <summary>
        /// Unix timestamp of when the user subscribed to the channel.
        /// </summary>
        public long? sub_created_at { get; set; }

        /// <summary>
        /// The level of the subscription
        /// </summary>
        public string sub_lv { get; set; }

        /// <summary>
        /// The tier of the subscription
        /// </summary>
        public string sub_tier { get; set; }
    }
}
