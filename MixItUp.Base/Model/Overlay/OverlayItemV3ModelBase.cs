﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Overlay
{
    public enum OverlayItemV3Type
    {
        Text,
        Image,
        Video,
        YouTube,
        HTML,
        Timer,
        Label,
        TwitchClip,
        StreamBoss,
        Goal,
        PersistentTimer,
        Chat,
        EndCredits,
    }

    public enum OverlayItemV3DisplayOptionsType
    {
        OverlayEndpoint,
        SingleWidgetURL,
    }

    public enum OverlayPositionV3Type
    {
        Simple,
        Percentage,
        Pixel,
        Random
    }

    [DataContract]
    public abstract class OverlayItemV3ModelBase : OverlayOutputV3Model
    {
        public const string MainDivElement = "document.getElementById('maindiv')";

        public const string InnerHTMLProperty = "InnerHTML";

        public static int zIndexCounter = 0;

        public static readonly string PositionedHTML = OverlayResources.OverlayPositionedItemDefaultHTML;
        public static readonly string PositionedCSS = OverlayResources.OverlayPositionedItemDefaultCSS;

        public static string GetPositionWrappedHTML(string innerHTML)
        {
            if (!string.IsNullOrEmpty(innerHTML))
            {
                return OverlayV3Service.ReplaceProperty(OverlayItemV3ModelBase.PositionedHTML, OverlayItemV3ModelBase.InnerHTMLProperty, innerHTML);
            }
            return innerHTML;
        }

        public static string GetPositionWrappedCSS(string innerCSS)
        {
            if (!string.IsNullOrEmpty(innerCSS))
            {
                return OverlayItemV3ModelBase.PositionedCSS + Environment.NewLine + Environment.NewLine + innerCSS;
            }
            return innerCSS;
        }

        [DataMember]
        public Guid OverlayEndpointID { get; set; }

        [DataMember]
        public OverlayItemV3Type Type { get; set; }

        [DataMember]
        public OverlayItemV3DisplayOptionsType DisplayOption { get; set; } = OverlayItemV3DisplayOptionsType.OverlayEndpoint;

        [DataMember]
        public int Width { get; set; }
        [DataMember]
        public int Height { get; set; }

        [DataMember]
        public OverlayPositionV3Type PositionType { get; set; }

        [DataMember]
        public int XPosition { get; set; }
        [DataMember]
        public int YPosition { get; set; }

        [DataMember]
        public int XMaximum { get; set; }
        [DataMember]
        public int YMaximum { get; set; }

        [DataMember]
        public int Layer { get; set; }

        [JsonIgnore]
        public bool PositionTypeIsPercentage { get { return this.PositionType == OverlayPositionV3Type.Simple || this.PositionType == OverlayPositionV3Type.Percentage; } }
        [JsonIgnore]
        public string PositionTypeUnit { get { return this.PositionTypeIsPercentage ? "%" : "px"; } }

        [JsonIgnore]
        public int XTranslation { get { return this.PositionTypeIsPercentage ? -50 : 0; } }
        [JsonIgnore]
        public int YTranslation { get { return this.PositionTypeIsPercentage ? -50 : 0; } }

        [JsonIgnore]
        public string SingleWidgetURL
        {
            get
            {
                OverlayEndpointV3Service widgetEndpoint = ServiceManager.Get<OverlayV3Service>().GetOverlayEndpointService(this.ID);
                if (widgetEndpoint != null)
                {
                    return widgetEndpoint.HttpAddress;
                }
                return null;
            }
        }

        [JsonIgnore]
        public virtual bool IsTestable { get { return false; } }
        [JsonIgnore]
        public virtual bool IsResettable { get { return false; } }

        protected OverlayItemV3ModelBase() { }

        public OverlayItemV3ModelBase(OverlayItemV3Type type) { this.Type = type; }

        public virtual Dictionary<string, object> GetGenerationProperties()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties[nameof(this.ID)] = (this.ID == Guid.Empty) ? Guid.NewGuid().ToString() : this.ID.ToString();
            properties[nameof(this.Width)] = (this.Width > 0) ? $"{this.Width}px" : "max-content";
            properties[nameof(this.Height)] = (this.Height > 0) ? $"{this.Height}px" : "max-content";

            if (this.Layer == 0)
            {
                zIndexCounter++;
            }

            if (this.PositionType == OverlayPositionV3Type.Random)
            {
                int x = RandomHelper.GenerateRandomNumber(this.XPosition, this.XMaximum);
                int y = RandomHelper.GenerateRandomNumber(this.YPosition, this.YMaximum);
                properties[nameof(this.XPosition)] = x;
                properties[nameof(this.YPosition)] = y;
            }
            else
            {
                properties[nameof(this.XPosition)] = this.XPosition;
                properties[nameof(this.YPosition)] = this.YPosition;
            }

            properties[nameof(this.PositionType)] = this.PositionType;
            properties[nameof(this.PositionTypeUnit)] = this.PositionTypeUnit;
            properties[nameof(this.XTranslation)] = this.XTranslation;
            properties[nameof(this.YTranslation)] = this.YTranslation;
            properties[nameof(this.Layer)] = (this.Layer == 0) ? zIndexCounter : this.Layer;

            return properties;
        }

        public virtual Task ProcessGenerationProperties(Dictionary<string, object> properties, CommandParametersModel parameters) { return Task.CompletedTask; }

        public async Task WidgetEnable()
        {
            await this.WidgetEnableInternal();

            await this.WidgetSendInitial();
        }

        public async Task WidgetDisable()
        {
            await this.WidgetDisableInternal();

            OverlayEndpointV3Service overlay = this.GetOverlayEndpointService();
            if (overlay != null)
            {
                await overlay.Remove(this.ID.ToString());
            }
        }

        public async Task WidgetReset()
        {
            await this.WidgetResetInternal();

            await this.WidgetDisable();

            await this.WidgetEnable();
        }

        public async Task WidgetSendInitial()
        {
            OverlayEndpointV3Service overlay = this.GetOverlayEndpointService();
            if (overlay != null)
            {
                Dictionary<string, object> properties = this.GetGenerationProperties();

                string iframeHTML = overlay.GetItemIFrameHTML();
                iframeHTML = OverlayV3Service.ReplaceProperty(iframeHTML, nameof(this.HTML), this.HTML);
                iframeHTML = OverlayV3Service.ReplaceProperty(iframeHTML, nameof(this.CSS), this.CSS);
                iframeHTML = OverlayV3Service.ReplaceProperty(iframeHTML, nameof(this.Javascript), this.Javascript);

                CommandParametersModel parametersModel = new CommandParametersModel();

                await this.ProcessGenerationProperties(properties, parametersModel);
                foreach (var property in properties)
                {
                    iframeHTML = OverlayV3Service.ReplaceProperty(iframeHTML, property.Key, property.Value);
                }

                iframeHTML = await SpecialIdentifierStringBuilder.ProcessSpecialIdentifiers(iframeHTML, parametersModel);

                await overlay.Add(this.ID.ToString(), iframeHTML);
            }
        }

        public virtual Task ProcessPacket(OverlayV3Packet packet) { return Task.CompletedTask; }

        protected virtual Task WidgetEnableInternal() { return Task.CompletedTask; }

        protected virtual Task WidgetDisableInternal() { return Task.CompletedTask; }

        protected virtual Task WidgetResetInternal() { return Task.CompletedTask; }

        protected async Task CallFunction(string functionName, Dictionary<string, object> data)
        {
            // TODO: Change to support different overlay endpoints or direct URLs
            OverlayEndpointV3Service overlay = this.GetOverlayEndpointService();
            if (overlay != null)
            {
                await overlay.Function(this.ID.ToString(), functionName, data);
            }
        }

        protected OverlayEndpointV3Service GetOverlayEndpointService()
        {
            if (this.DisplayOption == OverlayItemV3DisplayOptionsType.OverlayEndpoint)
            {
                return ServiceManager.Get<OverlayV3Service>().GetOverlayEndpointService(this.OverlayEndpointID);
            }
            else if (this.DisplayOption == OverlayItemV3DisplayOptionsType.SingleWidgetURL)
            {
                return ServiceManager.Get<OverlayV3Service>().GetOverlayEndpointService(this.ID);
            }
            return null;
        }
    }
}
