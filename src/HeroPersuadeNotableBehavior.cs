using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
        
        /// <summary>
        /// Checks 
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="settlement"></param>
        /// <returns>true, if hero cannot do revenge and persuasion direction should be 'to revenge'</returns>
        private bool GetPersuadeDirection(Hero hero)
        {
            return hero_trait_list_condition(hero,_cfg.values.peasantRevengerExcludeTrait);
        }

        private void SettlementEntered(MobileParty party,Settlement settlement,Hero hero)
        {
            if(settlement.IsVillage)
            {
                if(settlement.Notables != null && hero != null && !hero.IsHumanPlayerCharacter && hero.IsLord)
                {
                    if(settlement.Notables.Count > 0)
                    {/*TODO: Add lord persuade notables to rebel quest. 
                      * If lord has the quest to persuade - random element is not included.*/
                        bool lordHsNoPersuadeQuest = true;
                       
                        if(lordHsNoPersuadeQuest)
                        {
                            Random random = new Random((int)hero.Age);

                            if(random.Next (0 ,100) <= (_cfg.values.lordTryPersuadeNotableProbability * 100))
                            {
                                return;
                            }

                        }

                        bool to_revenge = GetPersuadeDirection(hero);                        

                        if(HeroWillTryToPersuadeTheNotable(hero,settlement,to_revenge,out Hero notable))
                        {
                            if(notable!=null)
                            {
                               
                                if(CheckConditions (hero ,notable ,_cfg.values.ai.lordPersuadeNotableChooseTeachTraitsAndRelationsWithSettlementOwner))
                                { 
                                    TeachHeroTraits (notable,_cfg.values.peasantRevengerExcludeTrait,!to_revenge);
                                   
                                    if(to_revenge)
                                    {
                                        log ($"{hero.Name} persuaded {notable.Name} to revenge");
                                    }
                                    else
                                    {
                                        log ($"{hero.Name} persuaded {notable.Name} not to revenge");
                                    }
                                }
                                else 
                                {
                                    if(can_remove_notable_from_village())
                                    {
                                        if (CheckConditions (hero ,notable ,_cfg.values.ai.lordPersuadeNotableChooseExecuteTraitsAndRelationsWithSettlementOwner))
                                        { 
                                            log ($"{hero.Name} killed {notable.Name}");                                           
                                            KillCharacterAction.ApplyByRemove (notable ,true ,true);
                                        }
                                        else if(CheckConditions (hero ,notable ,_cfg.values.ai.lordPersuadeNotableChooseExpelTraitsAndRelationsWithSettlementOwner))
                                        {
                                            log ($"{hero.Name} expeled {notable.Name}");
                                            KillCharacterAction.ApplyByRemove (notable ,true ,true);
                                        }
                                    }
                                }                            
                            }
                        }
                    }
                }
            }
        }



        private bool HeroWillTryToPersuadeTheNotable(Hero hero, Settlement settlement,bool direction_to_revenge, out Hero notable)
        {
            bool will_try = false;

            notable=null;
           
            for(int i = 0;i<settlement.Notables.Count;i++)
            {
                notable=settlement.Notables.ElementAt(i);

                bool notable_is_oposite = hero_trait_list_condition (notable ,_cfg.values.peasantRevengerExcludeTrait) != direction_to_revenge;

                if(notable_is_oposite)
                {
                    bool cannot_due_traits_and_relations_with_noble = CheckConditions(hero,notable,_cfg.values.ai.lordPersuadeNotableExcludeTraitsAndRelationsWithNotable); // lord cannot persuade notable in any way due to his traits and relations
                    bool cannot_due_traits_and_relations_with_settlement_owner = CheckConditions(hero,notable,_cfg.values.ai.lordPersuadeNotableExcludeTraitsAndRelationsWithSettlementOwner); // lord cannot persuade notable in any way due to his traits and relations

                    // hero personal interest
                    // traits of heroes should define their prefered direction of persuasion
                    // direction_to_revenge is already defined by hero. Here should be something to do with quest...
                    //bool revenge_to_lord = hero_trait_list_condition(hero,_cfg.values.lordRevengeToLordTraitsAndRelations);
                    //if(revenge_to_noble)
                    //{
                    //}
                    // kingdom interest
                    // direction may depend of kingdom interest
                    //bool different_faction = hero.MapFaction!=settlement.MapFaction;
                    //bool can_because_of_different_faction = different_faction && !direction_to_revenge || !different_faction && direction_to_revenge;

                    will_try = !cannot_due_traits_and_relations_with_noble && !cannot_due_traits_and_relations_with_settlement_owner;

                    if(will_try)
                    {
                        break;
                    }
                }
            }
            return will_try;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
