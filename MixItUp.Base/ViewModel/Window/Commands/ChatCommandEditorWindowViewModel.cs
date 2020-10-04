﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.Window.Commands
{
    public class ChatCommandEditorWindowViewModel : CommandEditorWindowViewModelBase
    {
        public string Triggers
        {
            get { return this.triggers; }
            set
            {
                this.triggers = value;
                this.NotifyPropertyChanged();
            }
        }
        private string triggers;

        public bool IncludeExclamation
        {
            get { return this.includeExclamation; }
            set
            {
                this.includeExclamation = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("ChatTriggersHintText");
            }
        }
        private bool includeExclamation;

        public bool IncludeExclamationEnabled
        {
            get { return this.includeExclamationEnabled; }
            set
            {
                this.includeExclamationEnabled = value;
                this.NotifyPropertyChanged();
            }
        }
        private bool includeExclamationEnabled;

        public bool Wildcards
        {
            get { return this.wildcards; }
            set
            {
                this.wildcards = value;
                this.IncludeExclamation = this.IncludeExclamationEnabled = !this.Wildcards;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("ChatTriggersHintText");
            }
        }
        private bool wildcards;

        public string ChatTriggersHintText
        {
            get
            {
                if (this.IncludeExclamation)
                {
                    return MixItUp.Base.Resources.ChatTriggersNoExclamationHintAssist;
                }
                else
                {
                    return MixItUp.Base.Resources.ChatTriggersHintAssist;
                }
            }
        }

        public ChatCommandEditorWindowViewModel(ChatCommandModel existingCommand)
            : base(existingCommand)
        {
            if (existingCommand.Triggers.Any(t => t.Contains(' ')))
            {
                this.Triggers = string.Join(";", existingCommand.Triggers);
            }
            else
            {
                this.Triggers = string.Join(" ", existingCommand.Triggers);
            }
            this.Wildcards = existingCommand.Wildcards;
            this.IncludeExclamation = existingCommand.IncludeExclamation;
        }

        public ChatCommandEditorWindowViewModel() : base() { }

        public override Task<Result> Validate()
        {
            if (string.IsNullOrEmpty(this.Name))
            {
                return Task.FromResult(new Result(MixItUp.Base.Resources.ACommandNameMustBeSpecified));
            }

            if (string.IsNullOrEmpty(this.Triggers))
            {
                return Task.FromResult(new Result(MixItUp.Base.Resources.ChatCommandMissingTriggers));
            }

            if (!ChatCommandModel.IsValidCommandTrigger(this.Triggers))
            {
                return Task.FromResult(new Result(MixItUp.Base.Resources.ChatCommandInvalidTriggers));
            }

            return Task.FromResult(new Result());
        }

        public override Task<CommandModelBase> GetCommand()
        {
            char[] triggerSeparator = new char[] { ' ' };
            if (this.Triggers.Contains(';'))
            {
                triggerSeparator = new char[] { ';' };
            }
            HashSet<string> triggers = new HashSet<string>(this.Triggers.Split(triggerSeparator, StringSplitOptions.RemoveEmptyEntries));

            return Task.FromResult<CommandModelBase>(new ChatCommandModel(this.Name, triggers, this.IncludeExclamation, this.Wildcards));
        }
    }
}