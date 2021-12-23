﻿using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Overlay
{
    [DataContract]
    public class OverlayGameQueueListItemModel : OverlayListItemModelBase
    {
        public const string HTMLTemplate =
        @"<div style=""position: relative; border-style: solid; border-width: 5px; border-color: {BORDER_COLOR}; background-color: {BACKGROUND_COLOR}; width: {WIDTH}px; height: {HEIGHT}px"">
          <p style=""position: absolute; top: 50%; left: 5%; float: left; text-align: left; font-family: '{TEXT_FONT}'; font-size: {TEXT_HEIGHT}px; color: {TEXT_COLOR}; white-space: nowrap; font-weight: bold; margin: auto; transform: translate(0, -50%);"">#{POSITION} {USERNAME}</p>
        </div>";

        private List<UserV2ViewModel> lastUsers = new List<UserV2ViewModel>();

        public OverlayGameQueueListItemModel() : base() { }

        public OverlayGameQueueListItemModel(string htmlText, int totalToShow, string textFont, int width, int height, string borderColor, string backgroundColor, string textColor,
            OverlayListItemAlignmentTypeEnum alignment, OverlayItemEffectEntranceAnimationTypeEnum addEventAnimation, OverlayItemEffectExitAnimationTypeEnum removeEventAnimation)
            : base(OverlayItemModelTypeEnum.GameQueue, htmlText, totalToShow, 0, textFont, width, height, borderColor, backgroundColor, textColor, alignment, addEventAnimation, removeEventAnimation)
        { }

        public override async Task LoadTestData()
        {
            UserV2ViewModel user = ChannelSession.User;

            List<UserV2ViewModel> users = new List<UserV2ViewModel>();
            for (int i = 0; i < this.TotalToShow; i++)
            {
                users.Add(user);
            }
            await this.AddGameQueueUsers(users);
        }

        public override async Task Enable()
        {
            GlobalEvents.OnGameQueueUpdated += GlobalEvents_OnGameQueueUpdated;

            await base.Enable();
        }

        public override async Task Disable()
        {
            this.lastUsers.Clear();

            GlobalEvents.OnGameQueueUpdated -= GlobalEvents_OnGameQueueUpdated;

            await base.Disable();
        }

        private async void GlobalEvents_OnGameQueueUpdated(object sender, System.EventArgs e)
        {
            await this.AddGameQueueUsers(ServiceManager.Get<GameQueueService>().Queue);
        }

        private async Task AddGameQueueUsers(IEnumerable<UserV2ViewModel> users)
        {
            await this.listSemaphore.WaitAndRelease(() =>
            {
                foreach (UserV2ViewModel user in this.lastUsers)
                {
                    if (!users.Contains(user))
                    {
                        this.Items.Add(OverlayListIndividualItemModel.CreateRemoveItem(user.ID.ToString()));
                    }
                }

                for (int i = 0; i < users.Count() && i < this.TotalToShow; i++)
                {
                    UserV2ViewModel user = users.ElementAt(i);

                    OverlayListIndividualItemModel item = OverlayListIndividualItemModel.CreateAddItem(user.ID.ToString(), user, i + 1, this.HTML);
                    item.TemplateReplacements.Add("USERNAME", (string)user.FullDisplayName);
                    item.TemplateReplacements.Add("POSITION", (i + 1).ToString());

                    this.Items.Add(item);
                }

                this.lastUsers = new List<UserV2ViewModel>(users);

                this.SendUpdateRequired();

                return Task.CompletedTask;
            });
        }
    }
}
