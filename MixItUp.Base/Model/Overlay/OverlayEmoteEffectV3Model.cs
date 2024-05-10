﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Services;
using MixItUp.Base.Services.External;
using MixItUp.Base.Services.Trovo;
using MixItUp.Base.Services.Twitch;
using MixItUp.Base.Services.YouTube;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat.Trovo;
using MixItUp.Base.ViewModel.Chat.Twitch;
using MixItUp.Base.ViewModel.Chat.YouTube;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Overlay
{
    public class OverlayEmoteEffectV3Model : OverlayItemV3ModelBase
    {
        public const string EmojiPrefix = "emoji://";

        public const string EmotesPropertyName = "Emotes";

        public static readonly string DefaultHTML = OverlayResources.OverlayImageDefaultHTML;
        public static readonly string DefaultCSS = OverlayResources.OverlayImageDefaultCSS;
        public static readonly string DefaultJavascript = OverlayResources.OverlayActionDefaultJavascript;

        [DataMember]
        public string EmoteText { get; set; }

        [DataMember]
        public bool AllowURLs { get; set; }
        [DataMember]
        public bool AllowEmoji { get; set; }

        public OverlayEmoteEffectV3Model() : base(OverlayItemV3Type.EmoteEffect) { }

        public override Dictionary<string, object> GetGenerationProperties()
        {
            Dictionary<string, object> properties = base.GetGenerationProperties();
            properties[EmotesPropertyName] = new List<string>();
            return properties;
        }

        public override async Task ProcessGenerationProperties(Dictionary<string, object> properties, CommandParametersModel parameters)
        {
            string emoteText = await SpecialIdentifierStringBuilder.ProcessSpecialIdentifiers(this.EmoteText, parameters);
            if (!string.IsNullOrWhiteSpace(emoteText))
            {
                emoteText = emoteText.ToLower();
                string[] splits = emoteText.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (splits != null && splits.Length > 0)
                {
                    List<string> emoteURLs = new List<string>();
                    foreach (string split in splits)
                    {
                        if (Uri.IsWellFormedUriString(split, UriKind.Absolute))
                        {
                            if (this.AllowURLs)
                            {
                                emoteURLs.Add(split);
                            }
                            continue;
                        }
                        else if (this.AllowEmoji && ModerationService.EmojiRegex.IsMatch(split))
                        {
                            emoteURLs.Add(EmojiPrefix + split);
                            continue;
                        }
                        else
                        {
                            if (StreamingPlatforms.ContainsPlatform(parameters.Platform, StreamingPlatformTypeEnum.Twitch))
                            {
                                if (ServiceManager.Get<TwitchChatService>().Emotes.TryGetValue(split, out TwitchChatEmoteViewModel twitchEmote))
                                {
                                    emoteURLs.Add(twitchEmote.ImageURL);
                                    continue;
                                }
                            }

                            if (StreamingPlatforms.ContainsPlatform(parameters.Platform, StreamingPlatformTypeEnum.YouTube))
                            {
                                if (ServiceManager.Get<YouTubeChatService>().EmoteDictionary.TryGetValue(split, out YouTubeChatEmoteViewModel youtubeEmote))
                                {
                                    emoteURLs.Add(youtubeEmote.ImageURL);
                                    continue;
                                }
                            }

                            if (StreamingPlatforms.ContainsPlatform(parameters.Platform, StreamingPlatformTypeEnum.Trovo))
                            {
                                if (ServiceManager.Get<TrovoChatEventService>().ChannelEmotes.TryGetValue(split, out TrovoChatEmoteViewModel trovoChannelEmote))
                                {
                                    emoteURLs.Add(trovoChannelEmote.ImageURL);
                                    continue;
                                }
                                else if (ServiceManager.Get<TrovoChatEventService>().EventEmotes.TryGetValue(split, out TrovoChatEmoteViewModel trovoEventEmote))
                                {
                                    emoteURLs.Add(trovoEventEmote.ImageURL);
                                    continue;
                                }
                                else if (ServiceManager.Get<TrovoChatEventService>().GlobalEmotes.TryGetValue(split, out TrovoChatEmoteViewModel trovoGlobalEmote))
                                {
                                    emoteURLs.Add(trovoGlobalEmote.ImageURL);
                                    continue;
                                }
                            }

                            if (ServiceManager.Get<BetterTTVService>().BetterTTVEmotes.TryGetValue(split, out BetterTTVEmoteModel bttvEmote))
                            {
                                emoteURLs.Add(bttvEmote.ImageURL);
                                continue;
                            }
                            else if (ServiceManager.Get<FrankerFaceZService>().FrankerFaceZEmotes.TryGetValue(split, out FrankerFaceZEmoteModel ffzEmote))
                            {
                                emoteURLs.Add(ffzEmote.ImageURL);
                                continue;
                            }
                        }
                    }

                    properties[EmotesPropertyName] = emoteURLs;
                }
            }
        }
    }
}
