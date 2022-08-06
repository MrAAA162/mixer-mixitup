﻿using MixItUp.Base.Model.Actions;
using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Overlay;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.Actions
{
    public enum OverlayActionTypeEnum
    {
        Text,
        Image,
        Video,
        YouTube,
        HTML,
        WebPage,
    }

    public class OverlayActionEditorControlViewModel : ActionEditorControlViewModelBase
    {
        public override ActionTypeEnum Type { get { return ActionTypeEnum.Overlay; } }

        public IEnumerable<OverlayActionTypeEnum> ActionTypes { get { return EnumHelper.GetEnumList<OverlayActionTypeEnum>(); } }

        public OverlayActionTypeEnum SelectedActionType
        {
            get { return this.selectedActionType; }
            set
            {
                this.selectedActionType = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(OverlayNotEnabled));
                this.NotifyPropertyChanged(nameof(OverlayEnabled));
                this.NotifyPropertyChanged(nameof(ShowTextItem));
                this.NotifyPropertyChanged(nameof(ShowImageItem));
                this.NotifyPropertyChanged(nameof(ShowVideoItem));
                this.NotifyPropertyChanged(nameof(ShowYouTubeItem));
                this.NotifyPropertyChanged(nameof(ShowHTMLItem));
                this.NotifyPropertyChanged(nameof(ShowWebPageItem));
            }
        }
        private OverlayActionTypeEnum selectedActionType;

        public bool OverlayNotEnabled { get { return !ServiceManager.Get<OverlayService>().IsConnected; } }

        public bool OverlayEnabled { get { return !this.OverlayNotEnabled; } }

        public IEnumerable<string> OverlayEndpoints { get { return ServiceManager.Get<OverlayService>().GetOverlayNames(); } }

        public string SelectedOverlayEndpoint
        {
            get { return this.selectedOverlayEndpoint; }
            set
            {
                var overlays = ServiceManager.Get<OverlayService>().GetOverlayNames();
                if (overlays.Contains(value))
                {
                    this.selectedOverlayEndpoint = value;
                }
                else
                {
                    this.selectedOverlayEndpoint = ServiceManager.Get<OverlayService>().DefaultOverlayName;
                }
                this.NotifyPropertyChanged();
            }
        }
        private string selectedOverlayEndpoint;

        public bool ShowTextItem { get { return this.SelectedActionType == OverlayActionTypeEnum.Text; } }

        public OverlayTextItemV3ViewModel TextItemViewModel
        {
            get { return this.textItemViewModel; }
            set
            {
                this.textItemViewModel = value;
                this.NotifyPropertyChanged();
            }
        }
        private OverlayTextItemV3ViewModel textItemViewModel = new OverlayTextItemV3ViewModel();

        public bool ShowImageItem { get { return this.SelectedActionType == OverlayActionTypeEnum.Image; } }

        public OverlayImageItemV3ViewModel ImageItemViewModel
        {
            get { return this.imageItemViewModel; }
            set
            {
                this.imageItemViewModel = value;
                this.NotifyPropertyChanged();
            }
        }
        private OverlayImageItemV3ViewModel imageItemViewModel = new OverlayImageItemV3ViewModel();

        public bool ShowVideoItem { get { return this.SelectedActionType == OverlayActionTypeEnum.Video; } }

        public OverlayVideoItemV3ViewModel VideoItemViewModel
        {
            get { return this.videoItemViewModel; }
            set
            {
                this.videoItemViewModel = value;
                this.NotifyPropertyChanged();
            }
        }
        private OverlayVideoItemV3ViewModel videoItemViewModel = new OverlayVideoItemV3ViewModel();

        public bool ShowYouTubeItem { get { return this.SelectedActionType == OverlayActionTypeEnum.YouTube; } }

        public OverlayYouTubeItemV3ViewModel YouTubeItemViewModel
        {
            get { return this.youTubeItemViewModel; }
            set
            {
                this.youTubeItemViewModel = value;
                this.NotifyPropertyChanged();
            }
        }
        private OverlayYouTubeItemV3ViewModel youTubeItemViewModel = new OverlayYouTubeItemV3ViewModel();

        public bool ShowHTMLItem { get { return this.SelectedActionType == OverlayActionTypeEnum.HTML; } }

        public OverlayHTMLItemV3ViewModel HTMLItemViewModel
        {
            get { return this.htmlItemViewModel; }
            set
            {
                this.htmlItemViewModel = value;
                this.NotifyPropertyChanged();
            }
        }
        private OverlayHTMLItemV3ViewModel htmlItemViewModel = new OverlayHTMLItemV3ViewModel();

        public bool ShowWebPageItem { get { return this.SelectedActionType == OverlayActionTypeEnum.WebPage; } }

        public OverlayWebPageItemV3ViewModel WebPageItemViewModel
        {
            get { return this.webPageItemViewModel; }
            set
            {
                this.webPageItemViewModel = value;
                this.NotifyPropertyChanged();
            }
        }
        private OverlayWebPageItemV3ViewModel webPageItemViewModel = new OverlayWebPageItemV3ViewModel();

        public OverlayActionEditorControlViewModel(OverlayActionModel action)
            : base(action)
        {
            if (!string.IsNullOrEmpty(action.OverlayName))
            {
                this.SelectedOverlayEndpoint = action.OverlayName;
            }
            else
            {
                this.SelectedOverlayEndpoint = ServiceManager.Get<OverlayService>().DefaultOverlayName;
            }

            if (action.OverlayItemV3 != null)
            {
                if (action.OverlayItemV3.Type == OverlayItemV3Type.Text)
                {
                    this.SelectedActionType = OverlayActionTypeEnum.Text;
                    this.TextItemViewModel = new OverlayTextItemV3ViewModel((OverlayTextItemV3Model)action.OverlayItemV3);
                }
                else if (action.OverlayItemV3.Type == OverlayItemV3Type.Image)
                {
                    this.SelectedActionType = OverlayActionTypeEnum.Image;
                    this.ImageItemViewModel = new OverlayImageItemV3ViewModel((OverlayImageItemV3Model)action.OverlayItemV3);
                }
                else if (action.OverlayItemV3.Type == OverlayItemV3Type.Video)
                {
                    this.SelectedActionType = OverlayActionTypeEnum.Video;
                    this.VideoItemViewModel = new OverlayVideoItemV3ViewModel((OverlayVideoItemV3Model)action.OverlayItemV3);
                }
                else if (action.OverlayItemV3.Type == OverlayItemV3Type.YouTube)
                {
                    this.SelectedActionType = OverlayActionTypeEnum.YouTube;
                    this.YouTubeItemViewModel = new OverlayYouTubeItemV3ViewModel((OverlayYouTubeItemV3Model)action.OverlayItemV3);
                }
                else if (action.OverlayItemV3.Type == OverlayItemV3Type.HTML)
                {
                    this.SelectedActionType = OverlayActionTypeEnum.HTML;
                    this.HTMLItemViewModel = new OverlayHTMLItemV3ViewModel((OverlayHTMLItemV3Model)action.OverlayItemV3);
                }
                else if (action.OverlayItemV3.Type == OverlayItemV3Type.WebPage)
                {
                    this.SelectedActionType = OverlayActionTypeEnum.WebPage;
                    this.WebPageItemViewModel = new OverlayWebPageItemV3ViewModel((OverlayWebPageItemV3Model)action.OverlayItemV3);
                }
            }
        }

        public OverlayActionEditorControlViewModel()
            : base()
        {
            this.SelectedOverlayEndpoint = ServiceManager.Get<OverlayService>().DefaultOverlayName;
        }

        public override Task<Result> Validate()
        {
            //if (this.SelectedActionType == OverlayActionTypeEnum.ShowHideWidget)
            //{
            //    //if (this.SelectedWidget == null)
            //    //{
            //    //    return Task.FromResult(new Result(MixItUp.Base.Resources.OverlayActionMissingWidget));
            //    //}
            //}
            //else
            //{
            //    //if (this.ItemDuration <= 0)
            //    //{
            //    //    return Task.FromResult(new Result(MixItUp.Base.Resources.OverlayActionDurationInvalid));
            //    //}

            //    //if (this.SelectedItemViewModel == null)
            //    //{
            //    //    return Task.FromResult(new Result(MixItUp.Base.Resources.OverlayActionItemInvalid));
            //    //}

            //    //OverlayItemModelBase overlayItem = this.SelectedItemViewModel.GetOverlayItem();
            //    //if (overlayItem == null)
            //    //{
            //    //    return Task.FromResult(new Result(MixItUp.Base.Resources.OverlayActionItemInvalid));
            //    //}
            //}
            return Task.FromResult(new Result());
        }

        protected override Task<ActionModelBase> GetActionInternal()
        {
            if (false)
            {

            }
            else
            {
                OverlayItemV3ViewModelBase itemViewModel = this.GetItemViewModel();
                if (itemViewModel != null)
                {
                    OverlayItemV3ModelBase item = itemViewModel.GetItem();
                    if (item != null)
                    {
                        return Task.FromResult<ActionModelBase>(new OverlayActionModel(this.SelectedOverlayEndpoint, item));
                    }
                }
            }
            return Task.FromResult<ActionModelBase>(null);
        }

        private OverlayItemV3ViewModelBase GetItemViewModel()
        {
            if (this.SelectedActionType == OverlayActionTypeEnum.Text)
            {
                return this.TextItemViewModel;
            }
            else if (this.SelectedActionType == OverlayActionTypeEnum.Image)
            {
                return this.ImageItemViewModel;
            }
            else if (this.SelectedActionType == OverlayActionTypeEnum.Video)
            {
                return this.VideoItemViewModel;
            }
            else if (this.SelectedActionType == OverlayActionTypeEnum.YouTube)
            {
                return this.YouTubeItemViewModel;
            }
            else if (this.SelectedActionType == OverlayActionTypeEnum.HTML)
            {
                return this.HTMLItemViewModel;
            }
            else if (this.SelectedActionType == OverlayActionTypeEnum.WebPage)
            {
                return this.WebPageItemViewModel;
            }
            return null;
        }
    }
}
