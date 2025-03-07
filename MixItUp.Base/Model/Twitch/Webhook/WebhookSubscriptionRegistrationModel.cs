﻿using Newtonsoft.Json;

namespace MixItUp.Base.Model.Twitch.Webhook
{
    /// <summary>
    /// The registration response for a web hook.
    /// </summary>
    public class WebhookSubscriptionRegistrationModel
    {
        /// <summary>
        /// URL where notifications will be delivered.
        /// </summary>
        [JsonProperty(PropertyName = "hub.callback")]
        public string callback { get; set; }

        /// <summary>
        /// Type of request. Valid values: subscribe, unsubscribe.
        /// </summary>
        [JsonProperty(PropertyName = "hub.mode")]
        public string mode { get; set; }

        /// <summary>
        /// URL for the topic to subscribe to or unsubscribe from. topic maps to a new Twitch API endpoint.
        /// </summary>
        [JsonProperty(PropertyName = "hub.topic")]
        public string topic { get; set; }

        /// <summary>
        /// Number of seconds until the subscription expires. Default: 0. Maximum: 864000. Should be specified to a value greater than 0 otherwise subscriptions will expire before any useful notifications can be sent.
        /// </summary>
        [JsonProperty(PropertyName = "hub.lease_seconds")]
        public int lease_seconds { get; set; }

        /// <summary>
        /// Secret used to sign notification payloads. The X-Hub-Signature header is generated by sha256(secret, notification_bytes). We strongly encourage you to use this, so your application can verify that notifications are genuine.
        /// </summary>
        [JsonProperty(PropertyName = "hub.secret")]
        public string secret { get; set; }

        /// <summary>
        /// Creates a new instance of the WebhookSubscriptionRegistrationModel class.
        /// </summary>
        public WebhookSubscriptionRegistrationModel() { }

        /// <summary>
        /// Creates a new instance of the WebhookSubscriptionRegistrationModel class.
        /// <param name="callback">URL where notifications will be delivered.</param>
        /// <param name="mode"></param>
        /// <param name="topic">URL for the topic to subscribe to or unsubscribe from. topic maps to a new Twitch API endpoint.</param>
        /// <param name="lease_seconds">Number of seconds until the subscription expires. Default: 0. Maximum: 864000.</param>
        /// <param name="secret">Secret used to sign notification payloads. The X-Hub-Signature header is generated by sha256(secret, notification_bytes).</param>
        /// </summary>
        public WebhookSubscriptionRegistrationModel(string callback, string mode, string topic, int lease_seconds, string secret)
        {
            this.callback = callback;
            this.mode = mode;
            this.topic = topic;
            this.lease_seconds = lease_seconds;
            this.secret = secret;
        }
    }
}
