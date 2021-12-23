﻿using MixItUp.Base;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Services;
using MixItUp.Base.ViewModel;
using MixItUp.Base.ViewModel.MainControls;
using MixItUp.WPF.Controls.Commands;
using MixItUp.WPF.Windows.Commands;
using System.Threading.Tasks;
using System.Windows;

namespace MixItUp.WPF.Controls.MainControls
{
    /// <summary>
    /// Interaction logic for TimerControl.xaml
    /// </summary>
    public partial class TimerControl : GroupedCommandsMainControlBase
    {
        private TimerMainControlViewModel viewModel;

        public TimerControl()
        {
            InitializeComponent();
        }

        protected override async Task InitializeInternal()
        {
            this.DataContext = this.viewModel = new TimerMainControlViewModel((MainWindowViewModel)this.Window.ViewModel);
            this.SetViewModel(this.viewModel);
            await this.viewModel.OnLoaded();

            await base.InitializeInternal();
        }

        private void NameFilterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.viewModel.NameFilter = this.NameFilterTextBox.Text;
        }

        private void CommandButtons_EditClicked(object sender, RoutedEventArgs e)
        {
            TimerCommandModel command = ((CommandListingButtonsControl)sender).GetCommandFromCommandButtons<TimerCommandModel>();
            if (command != null)
            {
                CommandEditorWindow window = CommandEditorWindow.GetCommandEditorWindow(command);
                window.CommandSaved += TimerWindow_CommandSaved;
                window.ForceShow();
            }
        }

        private async void CommandButtons_DeleteClicked(object sender, RoutedEventArgs e)
        {
            await this.Window.RunAsyncOperation(async () =>
            {
                TimerCommandModel command = ((CommandListingButtonsControl)sender).GetCommandFromCommandButtons<TimerCommandModel>();
                if (command != null)
                {
                    ServiceManager.Get<CommandService>().TimerCommands.Remove(command);
                    ChannelSession.Settings.RemoveCommand(command);
                    this.viewModel.RemoveCommand(command);
                    await ChannelSession.SaveSettings();

                    await ServiceManager.Get<TimerService>().RebuildTimerGroups();
                }
            });
        }

        private async void CommandButtons_EnableDisableToggled(object sender, RoutedEventArgs e)
        {
            await ServiceManager.Get<TimerService>().RebuildTimerGroups();
        }

        private void AddCommandButton_Click(object sender, RoutedEventArgs e)
        {
            CommandEditorWindow window = new CommandEditorWindow(CommandTypeEnum.Timer);
            window.CommandSaved += TimerWindow_CommandSaved;
            window.Show();
        }

        private async void TimerWindow_CommandSaved(object sender, CommandModelBase e)
        {
            await ServiceManager.Get<TimerService>().RebuildTimerGroups();
            Window_CommandSaved(sender, e);
        }
    }
}
