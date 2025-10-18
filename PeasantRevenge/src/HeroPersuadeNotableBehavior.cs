using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace PeasantRevenge
{
    internal class HeroPersuadeNotableBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this,SettlementEntered);
        }

        private void SettlementEntered(MobileParty party,Settlement settlement,Hero hero)
        {
            if(settlement.IsVillage)
            {
                if(settlement.Notables != null && hero != null)
                {
                    if(settlement.Notables.Count > 0)
                    {
                        int direction = GetPersuadeDirection(hero, settlement);

                        if(HeroWillTryToPersuadeTheNotable(hero, settlement, direction))
                        {
                            HeroPersuadeTheNotable(hero,settlement,direction);
                        }
                    }
                }
            }
        }

        private int GetPersuadeDirection(Hero hero,Settlement settlement)
        {
            return 0;
        }

        private bool HeroWillTryToPersuadeTheNotable(Hero hero, Settlement settlement,int direction)
        {
            bool will_try = false;
            // hero traits, relations

            // hero personal interest

            // kingdom interest

            return will_try;
        }



        private void HeroPersuadeTheNotable(Hero hero,Settlement settlement, int direction)
        {

        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
