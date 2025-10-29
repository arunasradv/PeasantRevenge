using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static PeasantRevenge.Common;

namespace PeasantRevenge
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (game.GameType is Campaign)
            {
                CampaignGameStarter campaignGameStarter = gameStarterObject as CampaignGameStarter;

                if (campaignGameStarter != null)
                {
                    LoadConfiguration(campaignGameStarter);
                    //campaignGameStarter.AddBehavior(new PeasantRevengeBehavior());
                    /*Quests*/
                    campaignGameStarter.AddBehavior(new NotableWantRevengeIssueBehavior());
                    /*Notable persuasions*/
                    campaignGameStarter.AddBehavior(new NotablePersuasionConversationsBehavior());
                    campaignGameStarter.AddBehavior(new HeroPersuadeNotableBehavior());
                    /*Other*/
                    campaignGameStarter.AddBehavior(new HelpNeutralVillageBehavior());
                }
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
        }

    }
}
