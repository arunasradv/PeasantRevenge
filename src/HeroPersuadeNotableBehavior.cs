using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using static PeasantRevenge.Common;

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
                if(settlement.Notables != null && hero != null && !hero.IsHumanPlayerCharacter && hero.IsLord)
                {
                    if(settlement.Notables.Count > 0)
                    {
                        bool not_to_revenge = GetPersuadeDirection(hero, settlement);

                        if(HeroWillTryToPersuadeTheNotable(hero,settlement,not_to_revenge,out Hero notable))
                        {
                            if(notable!=null)
                            {
                                TeachHeroTraits(notable,_cfg.values.peasantRevengerExcludeTrait,not_to_revenge);

                                if(not_to_revenge)
                                {
                                    log($"{hero.Name} persuaded {notable.Name} not to revenge");
                                }
                                else
                                {
                                    log($"{hero.Name} persuaded {notable.Name} to revenge");
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool GetPersuadeDirection(Hero hero,Settlement settlement)
        {
            return !hero_trait_list_condition(hero,_cfg.values.peasantRevengerExcludeTrait);
        }

        private bool HeroWillTryToPersuadeTheNotable(Hero hero, Settlement settlement,bool direction, out Hero notable)
        {
            bool will_try = false;

            notable=null;

            for(int i = 0;i<settlement.Notables.Count;i++)
            {
                notable=settlement.Notables.ElementAt(i);

                // hero traits, relations
                bool cannot_due_traits = CheckConditions(hero,notable,_cfg.values.ai.lordPersuadeNotableExcludeTraitsAndRelations); // lord cannot persuade notable in any way due to his traits and relations

                // hero personal interest

                //bool revenge_to_lord = hero_trait_list_condition(hero,_cfg.values.lordRevengeToLordTraitsAndRelations);
                //if(revenge_to_noble)
                //{

                //}
                // kingdom interest
                //bool different_faction = hero.MapFaction!=settlement.MapFaction;

                will_try=!cannot_due_traits;
                if(will_try)
                {
#warning add random propability, ...
                    break;
                }
            }
            return will_try;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
