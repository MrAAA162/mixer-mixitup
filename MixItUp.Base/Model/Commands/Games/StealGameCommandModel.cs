﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Commands.Games
{
    [DataContract]
    public class StealGameCommandModel : GameCommandModelBase
    {
        [DataMember]
        public GamePlayerSelectionType PlayerSelectionType { get; set; }

        [DataMember]
        public GameOutcomeModel SuccessfulOutcome { get; set; }

        [DataMember]
        public CustomCommandModel FailedCommand { get; set; }

        public StealGameCommandModel(string name, HashSet<string> triggers, GamePlayerSelectionType playerSelectionType, GameOutcomeModel successfulOutcome, CustomCommandModel failedCommand)
            : base(name, triggers, GameCommandTypeEnum.Steal)
        {
            this.PlayerSelectionType = playerSelectionType;
            this.SuccessfulOutcome = successfulOutcome;
            this.FailedCommand = failedCommand;
        }

        internal StealGameCommandModel(Base.Commands.StealGameCommand command)
            : base(command, GameCommandTypeEnum.Steal)
        {
            this.PlayerSelectionType = GamePlayerSelectionType.Random;
            this.SuccessfulOutcome = new GameOutcomeModel(command.SuccessfulOutcome);
            this.FailedCommand = new CustomCommandModel(command.FailedOutcome.Command) { IsEmbedded = true };
        }

        internal StealGameCommandModel(Base.Commands.PickpocketGameCommand command)
            : base(command, GameCommandTypeEnum.Steal)
        {
            this.PlayerSelectionType = GamePlayerSelectionType.Targeted;
            this.SuccessfulOutcome = new GameOutcomeModel(command.SuccessfulOutcome);
            this.FailedCommand = new CustomCommandModel(command.FailedOutcome.Command) { IsEmbedded = true };
        }

        private StealGameCommandModel() { }

        public override IEnumerable<CommandModelBase> GetInnerCommands()
        {
            List<CommandModelBase> commands = new List<CommandModelBase>();
            commands.Add(this.SuccessfulOutcome.Command);
            commands.Add(this.FailedCommand);
            return commands;
        }

        protected override async Task PerformInternal(CommandParametersModel parameters)
        {
            await this.SetSelectedUser(this.PlayerSelectionType, parameters);
            if (parameters.TargetUser != null)
            {
                if (this.ValidateTargetUserPrimaryBetAmount(parameters))
                {
                    int betAmount = this.GetPrimaryBetAmount(parameters);
                    parameters.SpecialIdentifiers[GameCommandModelBase.GamePayoutSpecialIdentifier] = betAmount.ToString();
                    if (this.GenerateProbability() <= this.SuccessfulOutcome.GetRoleProbabilityPayout(parameters.User).Probability)
                    {
                        this.PerformPrimarySetPayout(parameters.User, betAmount * 2);
                        this.PerformPrimarySetPayout(parameters.TargetUser, -betAmount);
                        await this.SuccessfulOutcome.Command.Perform(parameters);
                    }
                    else
                    {
                        await this.FailedCommand.Perform(parameters);
                    }
                    return;
                }
                else
                {
                    await ChannelSession.Services.Chat.SendMessage(MixItUp.Base.Resources.GameCommandTargetUserInvalidAmount);
                }
            }
            else
            {
                await ChannelSession.Services.Chat.SendMessage(MixItUp.Base.Resources.GameCommandCouldNotFindUser);
            }
            await this.Requirements.Refund(parameters);
        }
    }
}