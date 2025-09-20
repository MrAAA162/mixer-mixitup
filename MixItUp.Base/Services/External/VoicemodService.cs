using MixItUp.Base.Util;
using MixItUp.Base.Web;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace MixItUp.Base.Services.External
{
    public class VoicemodPacket
    {
        public string action { get; set; }
        public string id { get; set; } = Guid.NewGuid().ToString();
        public JObject payload { get; set; } = new JObject();

        public VoicemodPacket(string action)
        {
            this.action = action;
        }

        public VoicemodPacket(string action, JObject payload)
            : this(action)
        {
            this.payload = payload;
        }
    }

    public class VoicemodWebSocket : ClientWebSocketBase
    {
        private Dictionary<string, object> responses = new Dictionary<string, object>();

        public override Task<bool> Connect(string endpoint)
        {
            this.responses.Clear();
            return base.Connect(endpoint);
        }

        public async Task<JObject> SendAndReceive(VoicemodPacket packet, int delaySeconds = 5)
        {
            string serializedPacket = JSONSerializerHelper.SerializeToString(packet);
            Logger.Log(LogLevel.Debug, $"Voicemod Packet Sent - " + serializedPacket);

            this.responses.Clear();
            this.responses["next"] = null;

            await this.Send(serializedPacket);

            int cycles = delaySeconds * 10;
            object response = null;
            for (int i = 0; i < cycles && response == null; i++)
            {
                this.responses.TryGetValue("next", out response);
                await Task.Delay(100);
            }

            this.responses.Clear();
            return response as JObject;
        }

        protected override Task ProcessReceivedPacket(string packet)
        {
            try
            {
                Logger.Log(LogLevel.Debug, $"Voicemod Packet Received - " + packet);

                JObject response = JObject.Parse(packet);
                if (response != null)
                {
                    if (this.responses.ContainsKey("next"))
                    {
                        this.responses["next"] = response;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return Task.FromResult(0);
        }
    }

    /// <summary>
    /// Discord: https://discord.com/invite/vm-dev-community
    /// 
    /// v3 Documentation: https://control-api.voicemod.net
    /// </summary>
    public class VoicemodService : IVoicemodService
    {
        private static readonly List<int> AvailablePorts = new List<int>() { 59129, 20000, 39273, 42152, 43782, 46667, 35679, 37170, 38501, 33952, 30546 };

        public string Name { get { return MixItUp.Base.Resources.Voicemod; } }
        public bool IsConnected { get { return this.WebSocketConnected; } }
        public bool WebSocketConnected { get; private set; }

        private VoicemodWebSocket websocket = new VoicemodWebSocket();

        public VoicemodService() { }

        public async Task<Result> Connect()
        {
            try
            {
                string clientKey = ServiceManager.Get<SecretsService>().GetSecret("VoicemodV3ClientKey");

                foreach (int port in AvailablePorts)
                {
                    try
                    {
                        if (await this.websocket.Connect($"ws://localhost:{port}/v1/"))
                        {
                            JObject response = await this.websocket.SendAndReceive(new VoicemodPacket("registerClient", new JObject()
                            {
                                { "clientKey", clientKey }
                            }));

                            if (response != null && response["payload"] != null)
                            {
                                var payload = response["payload"] as JObject;
                                var status = payload["status"] as JObject;

                                if (status != null && status["code"]?.ToObject<int>() == 200)
                                {
                                    this.WebSocketConnected = true;
                                    this.websocket.OnDisconnectOccurred += Websocket_OnDisconnectOccurred;
                                    ServiceManager.Get<ITelemetryService>().TrackService("Voicemod");
                                    return new Result();
                                }
                            }
                        }
                        await this.websocket.Disconnect(WebSocketCloseStatus.NormalClosure);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }

                return new Result(MixItUp.Base.Resources.VoicemodConnectionFailed);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return new Result(MixItUp.Base.Resources.VoicemodConnectionFailed);
            }
        }

        public async Task Disconnect()
        {
            this.WebSocketConnected = false;
            this.websocket.OnDisconnectOccurred -= Websocket_OnDisconnectOccurred;
            await this.websocket.Disconnect();
        }

        public async Task<IEnumerable<VoicemodVoiceModel>> GetVoices()
        {
            Dictionary<string, VoicemodVoiceModel> results = new Dictionary<string, VoicemodVoiceModel>();

            JObject response = await this.websocket.SendAndReceive(new VoicemodPacket("getVoices"));

            if (response != null)
            {
                JToken actionObj = response["actionObject"];
                if (actionObj != null && actionObj["voices"] is JArray voices)
                {
                    foreach (JToken v in voices)
                    {
                        var voice = new VoicemodVoiceModel
                        {
                            voiceID = v["id"]?.ToString(),
                            friendlyName = v["friendlyName"]?.ToString(),
                            IsFavorite = v["favorited"]?.ToObject<bool>() ?? false,
                            IsCustom = v["isCustom"]?.ToObject<bool>() ?? false
                        };
                        results[voice.voiceID] = voice;
                    }

                    var favorites = results.Values.Where(v => v.IsFavorite).Select(v => v.friendlyName);
                    var customsz = results.Values.Where(v => v.IsCustom).Select(v => v.friendlyName);

                    Logger.Log(LogLevel.Debug, $"Favorite Voices: {string.Join(", ", favorites)}");
                    Logger.Log(LogLevel.Debug, $"Custom Voices: {string.Join(", ", customsz)}");
                }
            }

            return results.Values.ToList();
        }

        public async Task VoiceChangerOnOff(bool state)
        {
            JObject response = await this.websocket.SendAndReceive(new VoicemodPacket("getVoiceChangerStatus"));

            if (response != null)
            {
                JToken actionObj = response["actionObject"];
                if (actionObj != null && actionObj["value"] != null)
                {
                    bool current = actionObj["value"].ToObject<bool>();
                    if (current != state)
                    {
                        await this.websocket.SendAndReceive(new VoicemodPacket("toggleVoiceChanger"));
                    }
                }
            }
        }

        public async Task SelectVoice(string voiceID)
        {
            await this.websocket.SendAndReceive(new VoicemodPacket("loadVoice", new JObject
            {
                { "voiceID", voiceID }
            }));
        }

        public async Task RandomVoice(VoicemodRandomVoiceType voiceType)
        {
            await this.websocket.SendAndReceive(new VoicemodPacket("selectRandomVoice", new JObject
            {
                { "mode", voiceType.ToString() }
            }));
        }

        public async Task BeepSoundOnOff(bool state)
        {
            await this.websocket.SendAndReceive(new VoicemodPacket("setBeepSound", new JObject
            {
                { "badLanguage", state ? 1 : 0 }
            }));
        }

        public async Task HearMyselfOnOff(bool state)
        {
            JObject response = await this.websocket.SendAndReceive(new VoicemodPacket("getHearMyselfStatus"));

            if (response != null)
            {
                JToken actionObj = response["actionObject"];
                if (actionObj != null && actionObj["value"] != null)
                {
                    bool current = actionObj["value"].ToObject<bool>();
                    if (current != state)
                    {
                        await this.websocket.SendAndReceive(new VoicemodPacket("toggleHearMyVoice"));
                    }
                }
            }
        }

        public async Task MuteOnOff(bool state)
        {
            JObject response = await this.websocket.SendAndReceive(new VoicemodPacket("getMuteMicStatus"));

            if (response != null)
            {
                JToken actionObj = response["actionObject"];
                if (actionObj != null && actionObj["value"] != null)
                {
                    bool current = actionObj["value"].ToObject<bool>();
                    if (current != state)
                    {
                        await this.websocket.SendAndReceive(new VoicemodPacket("toggleMuteMic"));
                    }
                }
            }
        }

        public async Task<IEnumerable<VoicemodMemeModel>> GetMemeSounds()
        {
            List<VoicemodMemeModel> results = new List<VoicemodMemeModel>();

            JObject response = await this.websocket.SendAndReceive(new VoicemodPacket("getAllMemes"));

            if (response != null)
            {
                JToken actionObj = response["actionObject"];
                if (actionObj != null)
                {
                    JToken list = actionObj["listOfMemes"];
                    if (list != null && list is JArray)
                    {
                        foreach (JToken memeToken in (JArray)list)
                        {
                            var meme = new VoicemodMemeModel
                            {
                                Name = memeToken["Name"] != null ? memeToken["Name"].ToString() : null,
                                FileName = memeToken["Filename"] != null ? memeToken["Filename"].ToString() : null
                            };
                            results.Add(meme);
                        }
                    }
                }
            }

            return results;
        }

        public async Task PlayMemeSound(string fileName)
        {
            await this.websocket.SendAndReceive(new VoicemodPacket("playMeme", new JObject
            {
                { "FileName", fileName },
                { "IsKeyDown", true }
            }));
        }

        public async Task StopAllMemeSounds()
        {
            await this.websocket.SendAndReceive(new VoicemodPacket("stopAllMemeSounds"));
        }

        private async void Websocket_OnDisconnectOccurred(object sender, System.Net.WebSockets.WebSocketCloseStatus e)
        {
            ChannelSession.DisconnectionOccurred(MixItUp.Base.Resources.Voicemod);

            Result result = new Result();
            do
            {
                await this.Disconnect();
                await Task.Delay(5000);
                result = await this.Connect();
            }
            while (!result.Success);

            ChannelSession.ReconnectionOccurred(MixItUp.Base.Resources.Voicemod);
        }
    }
}