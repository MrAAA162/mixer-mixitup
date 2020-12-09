﻿using MixItUp.Base.Commands;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.ViewModel.Games;
using MixItUp.Base.ViewModel.Requirement;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MixItUp.WPF.Controls.Games
{
    /// <summary>
    /// Interaction logic for SpinGameEditorControl.xaml
    /// </summary>
    public partial class SpinGameEditorControl : GameEditorControlBase
    {
        private SpinGameCommandEditorWindowViewModel viewModel;
        private SpinGameCommand existingCommand;

        public SpinGameEditorControl(CurrencyModel currency)
        {
            InitializeComponent();

            this.viewModel = new SpinGameCommandEditorWindowViewModel(currency);
        }

        public SpinGameEditorControl(SpinGameCommand command)
        {
            InitializeComponent();

            this.existingCommand = command;
            //this.viewModel = new SpinGameCommandEditorWindowViewModel(command);
        }

        public override async Task<bool> Validate()
        {
            if (!await this.CommandDetailsControl.Validate())
            {

            }
            return false;
        }

        public override void SaveGameCommand()
        {
            //this.viewModel.SaveGameCommand(this.CommandDetailsControl.GameName, this.CommandDetailsControl.ChatTriggers, this.CommandDetailsControl.GetRequirements());
        }

        protected override async Task OnLoaded()
        {
            this.DataContext = this.viewModel;
            await this.viewModel.OnLoaded();

            if (this.existingCommand != null)
            {
                this.CommandDetailsControl.SetDefaultValues(this.existingCommand);
            }
            else
            {
                this.CommandDetailsControl.SetDefaultValues("Spin", "spin", CurrencyRequirementTypeEnum.MinimumAndMaximum, 10, 1000);
            }

            await base.OnLoaded();
        }

        private void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button button = (Button)sender;
            this.viewModel.DeleteOutcomeCommand.Execute(button.DataContext);
        }
    }
}