﻿using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using MixItUp.Base;
using MixItUp.Base.Model;
using MixItUp.Base.Services;
using MixItUp.Base.Services.External;
using MixItUp.Base.Util;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MixItUp.WPF.Services
{
    public class WindowsAmazonPollyService : ITextToSpeechConnectableService
    {
        public static readonly IEnumerable<TextToSpeechVoice> AvailableVoices = new List<TextToSpeechVoice>()
        {
            new TextToSpeechVoice("Aditi:standard", "Aditi"),
            new TextToSpeechVoice("Amy:standard", "Amy"),
            new TextToSpeechVoice("Astrid:standard", "Astrid"),
            new TextToSpeechVoice("Bianca:standard", "Bianca"),
            new TextToSpeechVoice("Brian:standard", "Brian"),
            new TextToSpeechVoice("Camila:standard", "Camila"),
            new TextToSpeechVoice("Carla:standard", "Carla"),
            new TextToSpeechVoice("Carmen:standard", "Carmen"),
            new TextToSpeechVoice("Celine:standard", "Céline"),
            new TextToSpeechVoice("Chantal:standard", "Chantal"),
            new TextToSpeechVoice("Conchita:standard", "Conchita"),
            new TextToSpeechVoice("Cristiano:standard", "Cristiano"),
            new TextToSpeechVoice("Dora:standard", "Dóra"),
            new TextToSpeechVoice("Emma:standard", "Emma"),
            new TextToSpeechVoice("Enrique:standard", "Enrique"),
            new TextToSpeechVoice("Ewa:standard", "Ewa"),
            new TextToSpeechVoice("Filiz:standard", "Filiz"),
            new TextToSpeechVoice("Geraint:standard", "Geraint"),
            new TextToSpeechVoice("Giorgio:standard", "Giorgio"),
            new TextToSpeechVoice("Gwyneth:standard", "Gwyneth"),
            new TextToSpeechVoice("Hans:standard", "Hans"),
            new TextToSpeechVoice("Ines:standard", "Inês"),
            new TextToSpeechVoice("Ivy:standard", "Ivy"),
            new TextToSpeechVoice("Jacek:standard", "Jacek"),
            new TextToSpeechVoice("Jan:standard", "Jan"),
            new TextToSpeechVoice("Joanna:standard", "Joanna"),
            new TextToSpeechVoice("Joey:standard", "Joey"),
            new TextToSpeechVoice("Justin:standard", "Justin"),
            new TextToSpeechVoice("Karl:standard", "Karl"),
            new TextToSpeechVoice("Kendra:standard", "Kendra"),
            new TextToSpeechVoice("Kimberly:standard", "Kimberly"),
            new TextToSpeechVoice("Lea:standard", "Léa"),
            new TextToSpeechVoice("Liv:standard", "Liv"),
            new TextToSpeechVoice("Lotte:standard", "Lotte"),
            new TextToSpeechVoice("Lucia:standard", "Lucia"),
            new TextToSpeechVoice("Lupe:standard", "Lupe"),
            new TextToSpeechVoice("Mads:standard", "Mads"),
            new TextToSpeechVoice("Maja:standard", "Maja"),
            new TextToSpeechVoice("Marlene:standard", "Marlene"),
            new TextToSpeechVoice("Mathieu:standard", "Mathieu"),
            new TextToSpeechVoice("Matthew:standard", "Matthew"),
            new TextToSpeechVoice("Maxim:standard", "Maxim"),
            new TextToSpeechVoice("Mia:standard", "Mia"),
            new TextToSpeechVoice("Miguel:standard", "Miguel"),
            new TextToSpeechVoice("Mizuki:standard", "Mizuki"),
            new TextToSpeechVoice("Naja:standard", "Naja"),
            new TextToSpeechVoice("Nicole:standard", "Nicole"),
            new TextToSpeechVoice("Penelope:standard", "Penélope"),
            new TextToSpeechVoice("Raveena:standard", "Raveena"),
            new TextToSpeechVoice("Ricardo:standard", "Ricardo"),
            new TextToSpeechVoice("Ruben:standard", "Ruben"),
            new TextToSpeechVoice("Russell:standard", "Russell"),
            new TextToSpeechVoice("Salli:standard", "Salli"),
            new TextToSpeechVoice("Seoyeon:standard", "Seoyeon"),
            new TextToSpeechVoice("Takumi:standard", "Takumi"),
            new TextToSpeechVoice("Tatyana:standard", "Tatyana"),
            new TextToSpeechVoice("Vicki:standard", "Vicki"),
            new TextToSpeechVoice("Vitoria:standard", "Vitória"),
            new TextToSpeechVoice("Zeina:standard", "Zeina"),
            new TextToSpeechVoice("Zhiyu:standard", "Zhiyu"),
        };

        public TextToSpeechProviderType ProviderType { get { return TextToSpeechProviderType.AmazonPolly; } }

        public int VolumeMinimum { get { return 0; } }

        public int VolumeMaximum { get { return 100; } }

        public int VolumeDefault { get { return 100; } }

        public int PitchMinimum { get { return 0; } }

        public int PitchMaximum { get { return 0; } }

        public int PitchDefault { get { return 0; } }

        public int RateMinimum { get { return 0; } }

        public int RateMaximum { get { return 0; } }

        public int RateDefault { get { return 0; } }

        private DateTimeOffset lastCommand = DateTimeOffset.MinValue;

        public bool IsUsingCustomAccessKey
        {
            get
            {
                return !string.IsNullOrEmpty(ChannelSession.Settings.AmazonPollyCustomRegionSystemName) &&
                    !string.IsNullOrEmpty(ChannelSession.Settings.AmazonPollyCustomAccessKey) &&
                    !string.IsNullOrEmpty(ChannelSession.Settings.AmazonPollyCustomSecretKey);
            }
        }

        public async Task<Result> TestAccess()
        {
            try
            {
                using (AmazonPollyClient client = this.GetClient())
                {
                    DescribeVoicesResponse response = await client.DescribeVoicesAsync(new DescribeVoicesRequest());
                    if (response != null && response.Voices != null && response.Voices.Count > 0)
                    {
                        return new Result();
                    }
                    return new Result(Resources.AmazonPollyNoVoicesReturned);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return new Result(ex);
            }
        }

        public IEnumerable<TextToSpeechVoice> GetVoices() { return WindowsAmazonPollyService.AvailableVoices; }

        public async Task Speak(string outputDevice, Guid overlayEndpointID, string text, string voice, int volume, int pitch, int rate, bool waitForFinish)
        {
            if (await this.IsWithinRateLimiting())
            {
                using (AmazonPollyClient client = this.GetClient())
                {
                    string[] splits = voice.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (splits.Length == 2)
                    {
                        string voiceID = splits[0];
                        string voiceEngine = splits[1];

                        SynthesizeSpeechResponse response = await client.SynthesizeSpeechAsync(new SynthesizeSpeechRequest()
                        {
                            VoiceId = voiceID,
                            Engine = new Engine(voiceEngine),
                            OutputFormat = OutputFormat.Mp3,
                            Text = text
                        });

                        if (response.HttpStatusCode == HttpStatusCode.OK && response.AudioStream != null)
                        {
                            MemoryStream stream = new MemoryStream();
                            using (response.AudioStream)
                            {
                                response.AudioStream.CopyTo(stream);
                                stream.Position = 0;
                            }
                            await ServiceManager.Get<IAudioService>().PlayMP3Stream(stream, volume, outputDevice, waitForFinish: waitForFinish);
                        }
                    }
                }
            }
        }

        private async Task<bool> IsWithinRateLimiting()
        {
            if (this.IsUsingCustomAccessKey || ChannelSession.IsDebug() || this.lastCommand.TotalMinutesFromNow() >= 5)
            {
                this.lastCommand = DateTimeOffset.Now;
                return true;
            }
            await ServiceManager.Get<ChatService>().SendMessage(string.Format(Resources.TextToSpeechActionBlockedDueToRateLimiting, Resources.AmazonPolly), StreamingPlatformTypeEnum.All);
            return false;
        }

        private AmazonPollyClient GetClient()
        {
            BasicAWSCredentials credentials = new BasicAWSCredentials(ServiceManager.Get<SecretsService>().GetSecret("AWSPollyAccessKey"), ServiceManager.Get<SecretsService>().GetSecret("AWSPollySecretKey"));
            RegionEndpoint region = RegionEndpoint.USWest2;
            if (this.IsUsingCustomAccessKey)
            {
                credentials = new BasicAWSCredentials(ChannelSession.Settings.AmazonPollyCustomAccessKey, ChannelSession.Settings.AmazonPollyCustomSecretKey);
                region = RegionEndpoint.GetBySystemName(ChannelSession.Settings.AmazonPollyCustomRegionSystemName);
            }
            return new AmazonPollyClient(credentials, region);
        }

        private async Task GenerateVoicesList()
        {
            using (AmazonPollyClient client = this.GetClient())
            {
                DescribeVoicesResponse voices = await client.DescribeVoicesAsync(new DescribeVoicesRequest());

                StringBuilder results = new StringBuilder();
                foreach (Voice v in voices.Voices)
                {
                    if (v.SupportedEngines.Count > 1)
                    {
                        foreach (string engine in v.SupportedEngines)
                        {
                            results.AppendLine($"new TextToSpeechVoice(\"{v.Id.Value}:{engine}\", \"{v.Name} - {engine}\"),");
                        }
                    }
                    else
                    {
                        results.AppendLine($"new TextToSpeechVoice(\"{v.Id.Value}:{v.SupportedEngines.First()}\", \"{v.Name}\"),");
                    }
                }

                await ServiceManager.Get<IFileService>().SaveFile("S:\\voices.txt", results.ToString());
            }
        }
    }
}
