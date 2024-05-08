﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Model.Overlay.Widgets;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModels;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.Overlay
{
    public class OverlayGoalSegmentV3ViewModel : UIViewModelBase
    {
        public string Name
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.NotifyPropertyChanged();
            }
        }
        private string name;

        public double Amount
        {
            get { return this.amount; }
            set
            {
                this.amount = Math.Max(value, 1);
                this.NotifyPropertyChanged();
            }
        }
        private double amount = 1;

        public ICommand DeleteCommand { get; private set; }

        private OverlayGoalV3ViewModel viewModel;

        public OverlayGoalSegmentV3ViewModel(OverlayGoalV3ViewModel viewModel)
        {
            this.viewModel = viewModel;
            this.DeleteCommand = this.CreateCommand(() =>
            {
                this.viewModel.DeleteSegment(this);
            });
        }

        public OverlayGoalSegmentV3ViewModel(OverlayGoalV3ViewModel viewModel, OverlayGoalSegmentV3Model segment)
            : this(viewModel)
        {
            this.Name = segment.Name;
            this.Amount = segment.Amount;
        }
    }

    public class OverlayGoalV3ViewModel : OverlayEventTrackingV3ViewModelBase
    {
        public override string DefaultHTML { get { return OverlayGoalV3Model.DefaultHTML; } }
        public override string DefaultCSS { get { return OverlayGoalV3Model.DefaultCSS; } }
        public override string DefaultJavascript { get { return OverlayGoalV3Model.DefaultJavascript; } }

        public override string EquationUnits { get { return Resources.Progress; } }

        public string Height
        {
            get { return this.height > 0 ? this.height.ToString() : string.Empty; }
            set
            {
                this.height = this.GetPositiveIntFromString(value);
                this.NotifyPropertyChanged();
            }
        }
        protected int height;

        public string BorderColor
        {
            get { return this.borderColor; }
            set
            {
                this.borderColor = value;
                this.NotifyPropertyChanged();
            }
        }
        private string borderColor;

        public string GoalColor
        {
            get { return this.goalColor; }
            set
            {
                this.goalColor = value;
                this.NotifyPropertyChanged();
            }
        }
        private string goalColor;

        public string ProgressColor
        {
            get { return this.progressColor; }
            set
            {
                this.progressColor = value;
                this.NotifyPropertyChanged();
            }
        }
        private string progressColor;

        public int StartingAmountCustom
        {
            get { return this.startingAmountCustom; }
            set
            {
                this.startingAmountCustom = value;
                this.NotifyPropertyChanged();
            }
        }
        private int startingAmountCustom;

        public IEnumerable<OverlayGoalSegmentV3Type> SegmentTypes { get; set; } = EnumHelper.GetEnumList<OverlayGoalSegmentV3Type>();

        public OverlayGoalSegmentV3Type SelectedSegmentType
        {
            get { return this.selectedSegmentType; }
            set
            {
                this.selectedSegmentType = value;
                this.NotifyPropertyChanged();
            }
        }
        private OverlayGoalSegmentV3Type selectedSegmentType = OverlayGoalSegmentV3Type.Cumulative;

        public ObservableCollection<OverlayGoalSegmentV3ViewModel> Segments { get; set; } = new ObservableCollection<OverlayGoalSegmentV3ViewModel>();

        public CustomCommandModel ProgressOccurredCommand
        {
            get { return this.progressOccurredCommand; }
            set
            {
                this.progressOccurredCommand = value;
                this.NotifyPropertyChanged();
            }
        }
        private CustomCommandModel progressOccurredCommand;

        public CustomCommandModel SegmentCompletedCommand
        {
            get { return this.segmentCompletedCommand; }
            set
            {
                this.segmentCompletedCommand = value;
                this.NotifyPropertyChanged();
            }
        }
        private CustomCommandModel segmentCompletedCommand;

        public ICommand AddSegmentCommand { get; private set; }

        public OverlayAnimationV3ViewModel ProgressOccurredAnimation;
        public OverlayAnimationV3ViewModel SegmentCompletedAnimation;

        public override bool IsTestable { get { return true; } }

        public OverlayGoalV3ViewModel()
            : base(OverlayItemV3Type.Goal)
        {
            this.FontSize = 16;

            this.Width = "400";
            this.Height = "100";

            this.BorderColor = "Black";
            this.GoalColor = "Red";
            this.ProgressColor = "Green";

            this.Segments.Add(new OverlayGoalSegmentV3ViewModel(this)
            {
                Name = "My Cool Goal",
                Amount = 1000
            });

            this.ProgressOccurredCommand = this.CreateEmbeddedCommand(Resources.ProgressOccurred);
            this.SegmentCompletedCommand = this.CreateEmbeddedCommand(Resources.SegmentCompleted);

            this.ProgressOccurredAnimation = new OverlayAnimationV3ViewModel(Resources.ProgressOccurred, new OverlayAnimationV3Model());
            this.SegmentCompletedAnimation = new OverlayAnimationV3ViewModel(Resources.SegmentCompleted, new OverlayAnimationV3Model());

            this.Animations.Add(this.ProgressOccurredAnimation);
            this.Animations.Add(this.SegmentCompletedAnimation);

            this.InitializeInternal();
        }

        public OverlayGoalV3ViewModel(OverlayGoalV3Model item)
            : base(item)
        {
            this.height = item.Height;

            this.BorderColor = item.BorderColor;
            this.GoalColor = item.GoalColor;
            this.ProgressColor = item.ProgressColor;

            this.StartingAmountCustom = item.StartingAmountCustom;
            this.SelectedSegmentType = item.SegmentType;

            foreach (OverlayGoalSegmentV3Model segment in item.Segments)
            {
                this.Segments.Add(new OverlayGoalSegmentV3ViewModel(this, segment));
            }

            this.ProgressOccurredCommand = this.GetEmbeddedCommand(item.ProgressOccurredCommandID, Resources.ProgressOccurred);
            this.SegmentCompletedCommand = this.GetEmbeddedCommand(item.SegmentCompletedCommandID, Resources.SegmentCompleted);

            this.ProgressOccurredAnimation = new OverlayAnimationV3ViewModel(Resources.ProgressOccurred, item.ProgressOccurredAnimation);
            this.SegmentCompletedAnimation = new OverlayAnimationV3ViewModel(Resources.SegmentCompleted, item.SegmentCompletedAnimation);

            this.Animations.Add(this.ProgressOccurredAnimation);
            this.Animations.Add(this.SegmentCompletedAnimation);

            this.InitializeInternal();
        }

        public void DeleteSegment(OverlayGoalSegmentV3ViewModel segment)
        {
            this.Segments.Remove(segment);
        }

        public override Result Validate()
        {
            if (this.Segments.Count == 0)
            {
                return new Result(Resources.OverlayGoalAtLeastOneSegmentMustBeAdded);
            }

            return new Result();
        }

        public override async Task TestWidget(OverlayWidgetV3Model widget)
        {
            OverlayGoalV3Model goal = (OverlayGoalV3Model)widget.Item;

            await goal.ProcessEvent(ChannelSession.User, goal.CurrentSegment.Amount / 2);

            await base.TestWidget(widget);
        }

        protected override OverlayItemV3ModelBase GetItemInternal()
        {
            OverlayGoalV3Model result = new OverlayGoalV3Model();

            this.AssignProperties(result);

            result.Height = this.height;

            result.BorderColor = this.BorderColor;
            result.GoalColor = this.GoalColor;
            result.ProgressColor = this.ProgressColor;

            result.StartingAmountCustom = this.StartingAmountCustom;
            result.SegmentType = this.SelectedSegmentType;

            result.Segments.Clear();
            foreach (OverlayGoalSegmentV3ViewModel segment in this.Segments)
            {
                result.Segments.Add(new OverlayGoalSegmentV3Model()
                {
                    Name = segment.Name,
                    Amount = segment.Amount,
                });
            }

            result.ProgressOccurredCommandID = this.ProgressOccurredCommand.ID;
            ChannelSession.Settings.SetCommand(this.ProgressOccurredCommand);

            result.SegmentCompletedCommandID = this.SegmentCompletedCommand.ID;
            ChannelSession.Settings.SetCommand(this.SegmentCompletedCommand);

            result.ProgressOccurredAnimation = this.ProgressOccurredAnimation.GetAnimation();
            result.SegmentCompletedAnimation = this.SegmentCompletedAnimation.GetAnimation();

            return result;
        }

        private void InitializeInternal()
        {
            this.AddSegmentCommand = this.CreateCommand(() =>
            {
                OverlayGoalSegmentV3ViewModel segment = new OverlayGoalSegmentV3ViewModel(this);
                segment.PropertyChanged += (sender, e) =>
                {
                    this.NotifyPropertyChanged("X");
                };
                this.Segments.Add(segment);
            });

            foreach (OverlayGoalSegmentV3ViewModel segment in this.Segments)
            {
                segment.PropertyChanged += (sender, e) =>
                {
                    this.NotifyPropertyChanged("X");
                };
            }
        }
    }
}
