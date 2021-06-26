﻿using MixItUp.Base;
using MixItUp.Base.Model.Actions;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.Store;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.CommunityCommands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace MixItUp.WPF.Windows.Commands
{
    /// <summary>
    /// Interaction logic for CommunityCommandUploadWindow.xaml
    /// </summary>
    public partial class CommunityCommandUploadWindow : LoadingWindowBase
    {
        private CommandModelBase command;
        private CommunityCommandDetailsViewModel ccCommand;

        private CommunityCommandUploadModel uploadCommand;

        private bool commandContainsMedia;

        public CommunityCommandUploadWindow(CommandModelBase command)
        {
            InitializeComponent();

            this.command = command;

            this.Initialize(this.StatusBar);
        }

        public CommunityCommandUploadWindow(CommunityCommandDetailsViewModel ccCommand)
        {
            InitializeComponent();

            this.ccCommand = ccCommand;

            this.Initialize(this.StatusBar);
        }

        protected override async Task OnLoaded()
        {
            await base.OnLoaded();

            try
            {
                if (this.command != null)
                {
                    this.uploadCommand = new CommunityCommandUploadModel()
                    {
                        ID = this.command.ID,
                        Name = this.command.Name,
                    };

                    this.uploadCommand.SetCommands(new List<CommandModelBase>() { this.command });

                    CommunityCommandDetailsModel existingCommand = await ChannelSession.Services.CommunityCommandsService.GetCommandDetails(command.ID);
                    if (existingCommand != null)
                    {
                        this.uploadCommand.ID = existingCommand.ID;
                        this.uploadCommand.Description = existingCommand.Description;
                    }

                    this.uploadCommand.Tags.Clear();
                    foreach (ActionModelBase action in this.command.Actions)
                    {
                        if (action is ConditionalActionModel)
                        {
                            foreach (ActionModelBase subAction in ((ConditionalActionModel)action).Actions)
                            {
                                this.uploadCommand.Tags.Add((CommunityCommandTagEnum)subAction.Type);
                            }
                        }
                        this.uploadCommand.Tags.Add((CommunityCommandTagEnum)action.Type);
                    }

                    switch (this.command.Type)
                    {
                        case CommandTypeEnum.Chat: this.uploadCommand.Tags.Add(CommunityCommandTagEnum.ChatCommand); break;
                        case CommandTypeEnum.Event: this.uploadCommand.Tags.Add(CommunityCommandTagEnum.EventCommand); break;
                        case CommandTypeEnum.ActionGroup: this.uploadCommand.Tags.Add(CommunityCommandTagEnum.ActionGroupCommand); break;
                        case CommandTypeEnum.Timer: this.uploadCommand.Tags.Add(CommunityCommandTagEnum.TimerCommand); break;
                        case CommandTypeEnum.Game: this.uploadCommand.Tags.Add(CommunityCommandTagEnum.GameCommand); break;
                        case CommandTypeEnum.StreamlootsCard: this.uploadCommand.Tags.Add(CommunityCommandTagEnum.StreamlootsCardCommand); break;
                        case CommandTypeEnum.TwitchChannelPoints: this.uploadCommand.Tags.Add(CommunityCommandTagEnum.TwitchChannelPointsCommand); break;
                    }
                }
                else
                {
                    this.uploadCommand = new CommunityCommandUploadModel()
                    {
                        ID = this.ccCommand.ID,
                        Name = this.ccCommand.Name,
                        Description = this.ccCommand.Description,
                    };
                }

                this.NameTextBox.Text = this.uploadCommand.Name;
                this.DescriptionTextBox.Text = this.uploadCommand.Description;

                if (this.uploadCommand.Tags.Contains(CommunityCommandTagEnum.Sound) || this.uploadCommand.Tags.Contains(CommunityCommandTagEnum.Overlay) ||
                    this.uploadCommand.Tags.Contains(CommunityCommandTagEnum.File) || this.uploadCommand.Tags.Contains(CommunityCommandTagEnum.ExternalProgram))
                {
                    this.commandContainsMedia = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void BrowseImageFilePathButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = ChannelSession.Services.FileService.ShowOpenFileDialog(ChannelSession.Services.FileService.ImageFileFilter());
            if (!string.IsNullOrEmpty(filePath))
            {
                this.ImageFilePathTextBox.Text = filePath;
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            await this.RunAsyncOperation(async () =>
            {
                try
                {
                    if (this.commandContainsMedia)
                    {
                        if (!await DialogHelper.ShowConfirmation(MixItUp.Base.Resources.CommunityCommandsExternalAssetActionsDetected))
                        {
                            return;
                        }
                    }

                    if (string.IsNullOrEmpty(this.NameTextBox.Text))
                    {
                        await DialogHelper.ShowMessage(MixItUp.Base.Resources.CommunityCommandsUploadInvalidName);
                        return;
                    }

                    if (string.IsNullOrEmpty(this.DescriptionTextBox.Text))
                    {
                        await DialogHelper.ShowMessage(MixItUp.Base.Resources.CommunityCommandsUploadInvalidDescription);
                        return;
                    }

                    if (!string.IsNullOrEmpty(this.ImageFilePathTextBox.Text))
                    {
                        if (!ChannelSession.Services.FileService.FileExists(this.ImageFilePathTextBox.Text))
                        {
                            await DialogHelper.ShowMessage(MixItUp.Base.Resources.CommunityCommandsUploadInvalidImageFile);
                            return;
                        }
                        this.uploadCommand.ImageFileData = await ChannelSession.Services.FileService.ReadFileAsBytes(this.ImageFilePathTextBox.Text);
                    }

                    this.uploadCommand.Name = this.NameTextBox.Text;
                    this.uploadCommand.Description = this.DescriptionTextBox.Text;

                    if (!await DialogHelper.ShowConfirmation(MixItUp.Base.Resources.CommunityCommandsUploadAgreement))
                    {
                        this.Close();
                        return;
                    }

                    if (!string.IsNullOrEmpty(this.ImageFilePathTextBox.Text) && ChannelSession.Services.FileService.FileExists(this.ImageFilePathTextBox.Text))
                    {
                        string imageFilePath = this.ImageFilePathTextBox.Text;
                        this.uploadCommand.ImageFileData = await Task.Run(() =>
                        {
                            try
                            {
                                using (var image = Image.Load(imageFilePath))
                                {
                                    image.Mutate(i => i.Resize(100, 100));
                                    using (MemoryStream memoryStream = new MemoryStream())
                                    {
                                        image.SaveAsPng(memoryStream);
                                        return memoryStream.ToArray();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex);
                            }
                            return null;
                        });
                    }

                    await ChannelSession.Services.CommunityCommandsService.AddOrUpdateCommand(this.uploadCommand);

                    this.Close();
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);

                    await DialogHelper.ShowMessage(MixItUp.Base.Resources.CommunityCommandsFailedToUploadCommand + ex.Message);
                }
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}