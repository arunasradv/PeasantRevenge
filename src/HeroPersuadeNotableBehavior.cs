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
        public override void RegisterEvents ()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener (this ,SettlementEntered);
        }

        /// <summary>
        /// Checks 
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="settlement"></param>
        /// <returns>true, if hero cannot do revenge and persuasion direction should be 'to revenge'</returns>
        private bool GetPersuadeDirection (Hero hero)
        {
            return hero_trait_list_condition (hero ,_cfg.values.peasantRevengerExcludeTrait);
        }

        private bool has_persuade_quest (Hero hero)
        {
            /*TODO: Add lord persuade notables to rebel quest.*/
            return true;
        }

        private bool check_probability (float chance ,int seed)
        {
            Random random = new Random(seed);

            if(random.Next (0 ,100) <= (chance * 100))
            {
                return false;
            }
            return true;
        }

        private void SettlementEntered (MobileParty party ,Settlement settlement ,Hero hero)
        {
            if(settlement.IsVillage)
            {
                if(settlement.Notables != null && hero != null && !hero.IsHumanPlayerCharacter && hero.IsLord)
                {
                    if(settlement.Notables.Count > 0)
                    {
                        bool lordHasPersuadeQuest = has_persuade_quest(hero);
                        float chance = lordHasPersuadeQuest ?
                            (float)(_cfg.values.lordTryPersuadeNotableProbability * 1.33):
                            (float)_cfg.values.lordTryPersuadeNotableProbability;                            

                        bool probability_condition = check_probability ( chance, (int)hero.Age);

                        if(probability_condition) {

                            bool to_revenge = GetPersuadeDirection(hero);

                            if(HeroWillTryToPersuadeTheNotable (hero ,settlement ,to_revenge ,out Hero notable))
                            {
                                if(notable != null)
                                {
                                    bool _teach = CheckConditions (hero ,notable ,_cfg.values.ai.lordPersuadeNotableChooseTeachTraitsAndRelationsWithSettlementOwner);
                                    bool _expel = CheckConditions (hero ,notable ,_cfg.values.ai.lordPersuadeNotableChooseExpelTraitsAndRelationsWithSettlementOwner);
                                    bool _kill = CheckConditions (hero ,notable ,_cfg.values.ai.lordPersuadeNotableChooseExecuteTraitsAndRelationsWithSettlementOwner);
                                    bool _bribe = false; /*TODO?*/
                                    persuade_type persuade_status = persuade_type.none;
                                    int task_index;                                   
                                    List<PeasantRevengeConfiguration.TraitAndValue> traits_values;
                                    int option_index;

                                    if(_teach)
                                    {
                                        persuade_status = to_revenge ? persuade_type.teach_to_revenge  : persuade_type.teach_to_not_revenge;
                                        task_index = GetTaskIndexByPersuadeStatus (persuade_status);
                                        option_index = GetOptionIndexByHeroTraits(hero ,task_index);
                                        traits_values = GetTraitsAndValuesByTaskAndOption (task_index ,option_index);
                                        OnLordPersuedeNotableUseTraitsAndValues (hero ,traits_values);

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
                                        if(can_remove_notable_from_village (notable))
                                        {
                                            if(_kill)
                                            {
                                                log ($"{hero.Name} killed {notable.Name} while seeking {(to_revenge ? "to revenge" : "to be pasive")}");
                                                KillCharacterAction.ApplyByRemove (notable ,true ,true);
                                            }
                                            else if(_expel)
                                            {
                                                persuade_status = persuade_type.accusation;
                                                task_index = GetTaskIndexByPersuadeStatus (persuade_status);
                                                option_index = GetOptionIndexByHeroTraits (hero ,task_index);
                                                traits_values = GetTraitsAndValuesByTaskAndOption (task_index ,option_index);
                                                OnLordPersuedeNotableUseTraitsAndValues (hero ,traits_values);
                                                log ($"{hero.Name} expeled {notable.Name}");
                                                KillCharacterAction.ApplyByRemove (notable ,true ,true);
                                            }
                                            else if(_bribe)
                                            {
                                                log ($"{hero.Name} bribed {notable.Name}");
                                            }
                                            else
                                            {
                                                //log ($"{hero.Name} took no action to remove {notable.Name} while seeking {(to_revenge ? "to revenge" : "to be pasive")}");
                                            }
                                        }
                                    }
                                    
                                   
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool HeroWillTryToPersuadeTheNotable (Hero hero ,Settlement settlement ,bool direction_to_revenge ,out Hero notable)
        {
            bool will_try = false;

            notable = null;

            for(int i = 0;i < settlement.Notables.Count;i++)
            {
                notable = settlement.Notables.ElementAt (i);

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
#if false
                    will_try = !cannot_due_traits_and_relations_with_noble && !cannot_due_traits_and_relations_with_settlement_owner;
#else
                    will_try = true;
#endif
                    if(will_try)
                    {
                        break;
                    } else {
                        if(cannot_due_traits_and_relations_with_noble)
                        log ($"{hero.Name} cannot_due_traits_and_relations_with_noble {notable.Name} persuade {(direction_to_revenge?"to revenge":"to be pasive")}");
                        if(cannot_due_traits_and_relations_with_settlement_owner)
                        log ($"{hero.Name} cannot_due_traits_and_relations_with_settlement_owner {notable.Name} persuade {(direction_to_revenge ? "to revenge" : "to be pasive")}");
                    }
                }
            }
            return will_try;
        }

        public override void SyncData (IDataStore dataStore)
        {
        }
    }
}
