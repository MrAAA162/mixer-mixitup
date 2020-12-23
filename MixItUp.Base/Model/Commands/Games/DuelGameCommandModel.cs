﻿using MixItUp.Base.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Commands.Games
{
    [DataContract]
    public class DuelGameCommandModel : GameCommandModelBase
    {
        [DataMember]
        public int TimeLimit { get; set; }
        [DataMember]
        public GamePlayerSelectionType PlayerSelectionType { get; set; }

        [DataMember]
        public CustomCommandModel StartedCommand { get; set; }

        [DataMember]
        public CustomCommandModel NotAcceptedCommand { get; set; }
        [DataMember]
        public GameOutcomeModel SuccessfulOutcome { get; set; }
        [DataMember]
        public CustomCommandModel FailedCommand { get; set; }

        [JsonIgnore]
        private bool gameActive = false;
        [JsonIgnore]
        private CommandParametersModel runParameters;
        [JsonIgnore]
        private CommandParametersModel targetParameters;
        [JsonIgnore]
        private CancellationTokenSource runCancellationTokenSource;

        public DuelGameCommandModel(string name, HashSet<string> triggers, int timeLimit, GamePlayerSelectionType playerSelectionType, CustomCommandModel startedCommand, CustomCommandModel notAcceptedCommand,
            GameOutcomeModel successfulOutcome, CustomCommandModel failedCommand)
            : base(name, triggers, GameCommandTypeEnum.Duel)
        {
            this.TimeLimit = timeLimit;
            this.PlayerSelectionType = playerSelectionType;
            this.StartedCommand = startedCommand;
            this.NotAcceptedCommand = notAcceptedCommand;
            this.SuccessfulOutcome = successfulOutcome;
            this.FailedCommand = failedCommand;
        }

        internal DuelGameCommandModel(Base.Commands.DuelGameCommand command)
            : base(command, GameCommandTypeEnum.Duel)
        {
            this.TimeLimit = command.TimeLimit;
            this.PlayerSelectionType = GamePlayerSelectionType.Targeted;
            this.StartedCommand = new CustomCommandModel(command.StartedCommand) { IsEmbedded = true };
            this.NotAcceptedCommand = new CustomCommandModel(command.NotAcceptedCommand) { IsEmbedded = true };
            this.SuccessfulOutcome = new GameOutcomeModel(command.SuccessfulOutcome);
            this.FailedCommand = new CustomCommandModel(command.FailedOutcome.Command) { IsEmbedded = true };
        }

        private DuelGameCommandModel() { }

        public override IEnumerable<CommandModelBase> GetInnerCommands()
        {
            List<CommandModelBase> commands = new List<CommandModelBase>();
            commands.Add(this.StartedCommand);
            commands.Add(this.NotAcceptedCommand);
            commands.Add(this.SuccessfulOutcome.Command);
            commands.Add(this.FailedCommand);
            return commands;
        }

        protected override async Task PerformInternal(CommandParametersModel parameters)
        {
            if (this.runCancellationTokenSource == null)
            {
                await this.SetSelectedUser(this.PlayerSelectionType, parameters);
                if (parameters.TargetUser != null)
                {
                    if (this.ValidateTargetUserPrimaryBetAmount(parameters))
                    {
                        this.runParameters = parameters;

                        this.runCancellationTokenSource = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        AsyncRunner.RunAsyncBackground(async (cancellationToken) =>
                        {
                            await Task.Delay(this.TimeLimit * 1000);

                            if (this.gameActive && cancellationToken != null && !cancellationToken.IsCancellationRequested)
                            {
                                this.gameActive = false;
                                await this.NotAcceptedCommand.Perform(parameters);
                                await this.Requirements.Refund(parameters);
                            }
                            await this.CooldownRequirement.Perform(parameters);
                            this.ClearData();
                        }, this.runCancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                        this.gameActive = true;
                        await this.StartedCommand.Perform(parameters);
                        this.ResetCooldown();
                        return;
                    }
                    else
                    {
                        await ChannelSession.Services.Chat.SendMessage(MixItUp.Base.Resources.TargetUserDoesNotHaveAmount);
                    }
                }
                else
                {
                    await ChannelSession.Services.Chat.SendMessage(MixItUp.Base.Resources.GameCommandCouldNotFindUser);
                }
            }
            else
            {
                if (this.runParameters != null && parameters.User == this.runParameters.TargetUser)
                {
                    this.targetParameters = parameters;

                    this.gameActive = false;
                    this.runParameters.SpecialIdentifiers[GameCommandModelBase.GamePayoutSpecialIdentifier] = this.GetPrimaryBetAmount(parameters).ToString();
                    if (this.GenerateProbability() <= this.SuccessfulOutcome.GetRoleProbabilityPayout(this.runParameters.User).Probability)
                    {
                        this.PerformPrimaryMultiplierPayout(this.runParameters, 2);
                        this.PerformPrimaryMultiplierPayout(this.targetParameters, -2);
                        await this.SuccessfulOutcome.Command.Perform(this.runParameters);
                    }
                    else
                    {
                        this.PerformPrimaryMultiplierPayout(this.targetParameters, 2);
                        this.PerformPrimaryMultiplierPayout(this.runParameters, -2);
                        await this.FailedCommand.Perform(this.runParameters);
                    }
                    await this.CooldownRequirement.Perform(this.runParameters);
                    this.ClearData();
                }
                else
                {
                    await ChannelSession.Services.Chat.SendMessage(MixItUp.Base.Resources.GameCommandAlreadyUnderway);
                }
            }
            await this.Requirements.Refund(parameters);
        }

        private void ClearData()
        {
            this.gameActive = false;
            this.runParameters = null;
            this.targetParameters = null;
            try
            {
                if (this.runCancellationTokenSource != null)
                {
                    this.runCancellationTokenSource.Cancel();
                }
            }
            catch (Exception) { }
            this.runCancellationTokenSource = null;
        }
    }
}