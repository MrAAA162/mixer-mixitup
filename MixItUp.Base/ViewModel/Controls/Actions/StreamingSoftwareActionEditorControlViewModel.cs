﻿using MixItUp.Base.Model.Actions;
using MixItUp.Base.Util;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.Controls.Actions
{
    public class StreamingSoftwareActionEditorControlViewModel : ActionEditorControlViewModelBase
    {
        public override ActionTypeEnum Type { get { return ActionTypeEnum.StreamingSoftware; } }

        public IEnumerable<StreamingSoftwareTypeEnum> StreamingSoftwares { get { return EnumHelper.GetEnumList<StreamingSoftwareTypeEnum>(); } }

        public StreamingSoftwareTypeEnum SelectedStreamingSoftware
        {
            get { return this.selectedStreamingSoftware; }
            set
            {
                this.selectedStreamingSoftware = value;
                this.NotifyPropertyChanged();
            }
        }
        private StreamingSoftwareTypeEnum selectedStreamingSoftware;

        public IEnumerable<StreamingSoftwareActionTypeEnum> ActionTypes { get { return EnumHelper.GetEnumList<StreamingSoftwareActionTypeEnum>(); } }

        public StreamingSoftwareActionTypeEnum SelectedActionType
        {
            get { return this.selectedActionType; }
            set
            {
                this.selectedActionType = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("ShowNotSupported");
                this.NotifyPropertyChanged("ShowSceneCollectionGrid");
                this.NotifyPropertyChanged("ShowSceneGrid");
                this.NotifyPropertyChanged("ShowSourceGrid");
                this.NotifyPropertyChanged("ShowTextSourceGrid");
                this.NotifyPropertyChanged("ShowWebBrowserSourceGrid");
                this.NotifyPropertyChanged("ShowSourceDimensionsGrid");
            }
        }
        private StreamingSoftwareActionTypeEnum selectedActionType;

        public bool OBSStudioNotEnabled { get { return !ChannelSession.Services.OBSStudio.IsConnected; } }

        public bool XSplitNotEnabled { get { return !ChannelSession.Services.XSplit.IsConnected; } }

        public bool StreamlabsOBSNotEnabled { get { return !ChannelSession.Services.StreamlabsOBS.IsConnected; } }

        public bool ShowNotSupported
        {
            get
            {
                // TODO
                return false;
            }
        }

        public bool ShowSceneCollectionGrid { get { return this.SelectedActionType == StreamingSoftwareActionTypeEnum.SceneCollection; } }

        public string SceneCollectionName
        {
            get { return this.sceneCollectionName; }
            set
            {
                this.sceneCollectionName = value;
                this.NotifyPropertyChanged();
            }
        }
        private string sceneCollectionName;

        public bool ShowSceneGrid { get { return this.SelectedActionType == StreamingSoftwareActionTypeEnum.Scene; } }

        public string SceneName
        {
            get { return this.sceneName; }
            set
            {
                this.sceneName = value;
                this.NotifyPropertyChanged();
            }
        }
        private string sceneName;

        public bool ShowSourceGrid { get { return this.SelectedActionType == StreamingSoftwareActionTypeEnum.SourceVisibility || this.ShowTextSourceGrid || this.ShowWebBrowserSourceGrid || this.ShowSourceDimensionsGrid; } }

        public string SourceName
        {
            get { return this.sourceName; }
            set
            {
                this.sourceName = value;
                this.NotifyPropertyChanged();
            }
        }
        private string sourceName;

        public bool SourceVisible
        {
            get { return this.sourceVisible; }
            set
            {
                this.sourceVisible = value;
                this.NotifyPropertyChanged();
            }
        }
        private bool sourceVisible;

        public bool ShowTextSourceGrid { get { return this.SelectedActionType == StreamingSoftwareActionTypeEnum.TextSource; } }

        public string SourceText
        {
            get { return this.sourceText; }
            set
            {
                this.sourceText = value;
                this.NotifyPropertyChanged();
            }
        }
        private string sourceText;

        public string SourceTextFilePath
        {
            get { return this.sourceTextFilePath; }
            set
            {
                this.sourceTextFilePath = value;
                this.NotifyPropertyChanged();
            }
        }
        private string sourceTextFilePath;

        public bool ShowWebBrowserSourceGrid { get { return this.SelectedActionType == StreamingSoftwareActionTypeEnum.WebBrowserSource; } }

        public string SourceWebPageFilePath
        {
            get { return this.sourceWebPageFilePath; }
            set
            {
                this.sourceWebPageFilePath = value;
                this.NotifyPropertyChanged();
            }
        }
        private string sourceWebPageFilePath;

        public bool ShowSourceDimensionsGrid { get { return this.SelectedActionType == StreamingSoftwareActionTypeEnum.SourceDimensions; } }

        public int SourceXPosition
        {
            get { return this.sourceXPosition; }
            set
            {
                this.sourceXPosition = value;
                this.NotifyPropertyChanged();
            }
        }
        private int sourceXPosition;

        public int SourceYPosition
        {
            get { return this.sourceYPosition; }
            set
            {
                this.sourceYPosition = value;
                this.NotifyPropertyChanged();
            }
        }
        private int sourceYPosition;

        public int SourceRotation
        {
            get { return this.sourceRotation; }
            set
            {
                this.sourceRotation = value;
                this.NotifyPropertyChanged();
            }
        }
        private int sourceRotation;

        public float SourceXScale
        {
            get { return this.sourceXScale; }
            set
            {
                this.sourceXScale = value;
                this.NotifyPropertyChanged();
            }
        }
        private float sourceXScale;

        public float SourceYScale
        {
            get { return this.sourceYScale; }
            set
            {
                this.sourceYScale = value;
                this.NotifyPropertyChanged();
            }
        }
        private float sourceYScale;

        public ICommand SourceGetCurrentDimensionsCommand { get; private set; }

        public StreamingSoftwareActionEditorControlViewModel(StreamingSoftwareActionModel action)
            : this()
        {
            this.SelectedStreamingSoftware = action.StreamingSoftwareType;
            this.SelectedActionType = action.ActionType;
            if (this.ShowSceneCollectionGrid)
            {
                this.SceneCollectionName = action.ItemName;
            }
            else if (this.ShowSceneGrid)
            {
                this.SceneName = action.ItemName;
            }
            else if (this.ShowSourceGrid)
            {
                this.SceneName = action.ParentName;
                this.SourceName = action.ItemName;
                this.SourceVisible = action.Visible;
                if (this.ShowTextSourceGrid)
                {
                    this.SourceText = action.SourceText;
                    this.SourceTextFilePath = action.SourceTextFilePath;
                }
                else if (this.ShowWebBrowserSourceGrid)
                {
                    this.SourceWebPageFilePath = action.SourceURL;
                }
                else if (this.ShowSourceDimensionsGrid)
                {
                    this.SourceXPosition = action.SourceDimensions.X;
                    this.SourceYPosition = action.SourceDimensions.Y;
                    this.SourceRotation = action.SourceDimensions.Rotation;
                    this.SourceXScale = action.SourceDimensions.XScale;
                    this.SourceYScale = action.SourceDimensions.YScale;
                }
            }
        }

        public StreamingSoftwareActionEditorControlViewModel()
        {
            this.SourceGetCurrentDimensionsCommand = this.CreateCommand(async (parameter) =>
            {
                if (string.IsNullOrEmpty(this.SourceName))
                {
                    StreamingSoftwareSourceDimensionsModel dimensions = await StreamingSoftwareActionModel.GetSourceDimensions(this.SelectedStreamingSoftware, this.SceneName, this.SourceName);
                    if (dimensions != null)
                    {
                        this.SourceXPosition = dimensions.X;
                        this.SourceYPosition = dimensions.Y;
                        this.SourceRotation = dimensions.Rotation;
                        this.SourceXScale = dimensions.XScale;
                        this.SourceYScale = dimensions.YScale;
                    }
                }
            });
        }

        public override Task<Result> Validate()
        {
            if (this.ShowSceneCollectionGrid)
            {
                if (string.IsNullOrEmpty(this.SceneCollectionName))
                {
                    return Task.FromResult(new Result(MixItUp.Base.Resources.StreamingSoftwareActionMissingSceneCollection));
                }
            }
            else if (this.ShowSceneGrid)
            {
                if (string.IsNullOrEmpty(this.SceneName))
                {
                    return Task.FromResult(new Result(MixItUp.Base.Resources.StreamingSoftwareActionMissingScene));
                }
            }
            else if (this.ShowSourceGrid)
            {
                if (string.IsNullOrEmpty(this.SourceName))
                {
                    return Task.FromResult(new Result(MixItUp.Base.Resources.StreamingSoftwareActionMissingSource));
                }

                if (this.ShowTextSourceGrid)
                {
                    if (string.IsNullOrEmpty(this.SourceTextFilePath))
                    {
                        return Task.FromResult(new Result(MixItUp.Base.Resources.StreamingSoftwareActionMissingTextSourceFilePath));
                    }
                }
                else if (this.ShowWebBrowserSourceGrid)
                {
                    if (string.IsNullOrEmpty(this.SourceWebPageFilePath))
                    {
                        return Task.FromResult(new Result(MixItUp.Base.Resources.StreamingSoftwareActionMissingWebBrowserSourceFilePath));
                    }
                }
                else if (this.ShowSourceDimensionsGrid)
                {

                }
            }
            return Task.FromResult(new Result());
        }

        public override Task<ActionModelBase> GetAction()
        {
            if (this.ShowSceneCollectionGrid)
            {
                return Task.FromResult<ActionModelBase>(StreamingSoftwareActionModel.CreateSceneCollectionAction(this.SelectedStreamingSoftware, this.SceneCollectionName));
            }
            else if (this.ShowSceneGrid)
            {
                return Task.FromResult<ActionModelBase>(StreamingSoftwareActionModel.CreateSceneAction(this.SelectedStreamingSoftware, this.SceneName));
            }
            else if (this.ShowSourceGrid)
            {
                if (this.ShowTextSourceGrid)
                {
                    return Task.FromResult<ActionModelBase>(StreamingSoftwareActionModel.CreateTextSourceAction(this.SelectedStreamingSoftware, this.SceneName, this.SourceName, this.SourceVisible, this.SourceText, this.SourceTextFilePath));
                }
                else if (this.ShowWebBrowserSourceGrid)
                {
                    return Task.FromResult<ActionModelBase>(StreamingSoftwareActionModel.CreateWebBrowserSourceAction(this.SelectedStreamingSoftware, this.SceneName, this.SourceName, this.SourceVisible, this.SourceWebPageFilePath));
                }
                else if (this.ShowSourceDimensionsGrid)
                {
                    return Task.FromResult<ActionModelBase>(StreamingSoftwareActionModel.CreateSourceDimensionsAction(this.SelectedStreamingSoftware, this.SceneName, this.SourceName, this.SourceVisible,
                        new StreamingSoftwareSourceDimensionsModel(this.SourceXPosition, this.SourceYPosition, this.SourceRotation, this.SourceXScale, this.SourceYScale)));
                }
                else
                {
                    return Task.FromResult<ActionModelBase>(StreamingSoftwareActionModel.CreateSourceVisibilityAction(this.SelectedStreamingSoftware, this.SceneName, this.SourceName, this.SourceVisible));
                }
            }
            return Task.FromResult<ActionModelBase>(null);
        }
    }
}
