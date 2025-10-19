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
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, SettlementEntered);
        }
        /// <summary>
        /// Checks 
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="settlement"></param>
        /// <returns>true, if hero can do revenge 
        /// persuasion direction should be 'to revenge'</returns>
        private bool GetHeroPreferedPersuadeDirection(Hero hero)
        {
            bool hero_tend_to_revenge = !hero_trait_list_condition(hero, _cfg.values.peasantRevengerExcludeTrait);

            return hero_tend_to_revenge;
        }

        private bool Has_persuade_quest(Hero hero)
        {
            /*TODO: Add lord persuade notables to rebel quest.*/
            return true;
        }

        private bool check_probability(float chance, int seed)
        {
            Random random = new Random(seed);

            if (random.Next(0, 100) <= (chance * 100))
            {
                return false;
            }
            return true;
        }

        private void SettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            bool condition_for_village_notables = settlement.IsVillage &&
                settlement.Notables != null &&
                hero != null &&
                !hero.IsHumanPlayerCharacter &&
                hero.IsLord && settlement.Notables.Count > 0;

            if (condition_for_village_notables)
            {
                bool lordHasPersuadeQuest = Has_persuade_quest(hero);
                float chance = lordHasPersuadeQuest ?
                            (float)(_cfg.values.lordTryPersuadeNotableProbability * 1.33) :
                            (float)_cfg.values.lordTryPersuadeNotableProbability;

                bool probability_condition = check_probability(chance, (int)hero.Age);

                if (probability_condition)
                {

                    bool to_revenge = GetHeroPreferedPersuadeDirection(hero);

                    if (HeroWillTryToPersuadeTheNotable(hero, settlement, to_revenge, out Hero notable))
                    {
                        if (notable != null)
                        {
                            persuade_type persuaded = persuade_type.none;
                            bool _teach = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableChooseTeachTraitsAndRelationsWithSettlementOwner);
                            bool _expel = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableChooseExpelTraitsAndRelationsWithSettlementOwner);
                            bool _kill = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableChooseExecuteTraitsAndRelationsWithSettlementOwner);
                            bool _bribe = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableChooseBribeTraitsAndRelationsWithSettlementOwner);

                            int goldNeeded = get_notable_bribe_amount(notable);
                            bool money_con = CanAffordToSpendMoney(hero, goldNeeded, _cfg.values.ai.lordPersuadeNotableWillAffordPartOfHisSavingsToPayForBribe);
                            bool will_accept_bribe = CheckConditions(notable, hero, _cfg.values.ai.notableWillAcceptTheBribe);
                            bool traits_allow = CheckOnlyTraitsConditions(notable, hero, _cfg.values.ai.notableWillAcceptTheBribe);

                            persuade_type persuade_status = persuade_type.none;
                            int task_index;
                            List<PeasantRevengeConfiguration.TraitAndValue> traits_values;
                            int option_index;

                            if (_teach)
                            {
                                TeachHeroTraits(notable, _cfg.values.peasantRevengerExcludeTrait, !to_revenge);
                                persuade_status = to_revenge ? persuade_type.teach_to_revenge : persuade_type.teach_to_not_revenge;
                                task_index = GetTaskIndexByPersuadeStatus(persuade_status);
                                option_index = GetOptionIndexByHeroTraits(hero, task_index);
                                traits_values = GetTraitsAndValuesByTaskAndOption(task_index, option_index);
                                OnLordUseTraitsAndValues(hero, traits_values);
                                log($"{hero.Name} persuaded {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) {(to_revenge ? "to revenge" : "to be pasive")}.");
                                persuaded = persuade_type.show_example_success;
                            }

                            if (_bribe && persuaded == persuade_type.none)
                            {
                                if (money_con && will_accept_bribe && traits_allow)
                                {
                                    TeachHeroTraits(notable, _cfg.values.peasantRevengerExcludeTrait, !to_revenge);
                                    GiveGoldAction.ApplyBetweenCharacters(hero, notable, goldNeeded, false);
                                    log($"{hero.Name} bribed {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) {(to_revenge ? "to revenge" : "to be pasive")}.");
                                    persuaded = persuade_type.bribe_success;
                                }
                                else if (!will_accept_bribe && traits_allow)
                                {
                                    GiveGoldAction.ApplyBetweenCharacters(hero, notable, goldNeeded, false);
                                    log($"{hero.Name} failed to bribe {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) {(to_revenge ? "to revenge" : "to be pasive")}.");
                                    persuaded = persuade_type.bribe_fail;
                                }
                                else
                                {
                                    if (!money_con)
                                    {
                                        log($"{hero.Name} do not have money to bribe the {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) {(to_revenge ? "to revenge" : "to be pasive")}.");
                                    }
                                    else if (!will_accept_bribe)
                                    {
                                        log($" {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) does not accepted the bribe ({(to_revenge ? "to revenge" : "to be pasive")}) from {hero.Name}.");
                                    }
                                    else if (!traits_allow)
                                    {
                                        log($"{hero.Name} did not bribed ({(to_revenge ? "to revenge" : "to be pasive")}) the {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}), because traits does not allow bribing.");
                                    }
                                }
                            }

                            if (persuaded == persuade_type.none)
                            {
                                if (can_remove_notable_from_village(notable))
                                {
                                    if (_expel)
                                    {
                                        persuade_status = persuade_type.accusation;
                                        task_index = GetTaskIndexByPersuadeStatus(persuade_status);
                                        option_index = GetOptionIndexByHeroTraits(hero, task_index);
                                        traits_values = GetTraitsAndValuesByTaskAndOption(task_index, option_index);
                                        OnLordUseTraitsAndValues(hero, traits_values);
                                        log($"{hero.Name} expeled {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) {(to_revenge ? "to revenge" : "to be pasive")}.");
                                        KillCharacterAction.ApplyByRemove(notable, true, true);
                                        persuaded = persuade_type.expeled;
                                    }

                                    if (_kill && persuaded == persuade_type.none)
                                    {
                                        OnHeroChopNotableHeadConsequence(hero, notable);
                                        log($"{hero.Name} killed {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) while seeking {(to_revenge ? "to revenge" : "to be pasive")}.");
                                        KillCharacterAction.ApplyByRemove(notable, true, true);
                                        persuaded = persuade_type.killed;
                                    }

                                    if (persuaded == persuade_type.none)
                                    {
                                        // log ($"{hero.Name} took no action to remove {notable.Name} while seeking {(to_revenge ? "to revenge" : "to be pasive")}.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool HeroWillTryToPersuadeTheNotable(Hero hero, Settlement settlement, bool direction_to_revenge, out Hero notable)
        {
            bool will_try = false;

            notable = null;

            for (int i = 0; i < settlement.Notables.Count; i++)
            {
                notable = settlement.Notables.ElementAt(i);

                bool notable_is_oposite = hero_trait_list_condition(notable, _cfg.values.peasantRevengerExcludeTrait) != direction_to_revenge;

                if (notable_is_oposite) // it means notable peasant is doing the opposite to hero preferred circumstances.
                {

                    bool cannot_due_traits_and_relations_with_noble = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableExcludeTraitsAndRelationsWithNotable); // lord cannot persuade notable in any way due to his traits and relations
                    bool cannot_due_traits_and_relations_with_settlement_owner = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableExcludeTraitsAndRelationsWithSettlementOwner); // lord cannot persuade notable in any way due to his traits and relations

                    bool at_war = hero.MapFaction.IsAtWarWith(notable.MapFaction);
                    bool same_faction = hero.MapFaction == notable.MapFaction;
                    bool approve_revenge = CheckConditions(hero, notable, _cfg.values.ai.lordTraitsApprovePeasantsPower);
                    bool oppose_revenge = CheckConditions(hero, notable, _cfg.values.ai.lordTraitsOpposingPeasantsPower);

                    if (approve_revenge == false && oppose_revenge == false)
                    {
                        //choose random or stick to the quest task
                    }

                    if (direction_to_revenge)
                    {
                        will_try = CheckConditions(hero, notable, _cfg.values.ai.lordTraitsApprovePeasantsPower);
                        if (same_faction)
                        {

                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        will_try &= at_war || !same_faction;
                    }


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
#if true
                    will_try &= !cannot_due_traits_and_relations_with_noble && !cannot_due_traits_and_relations_with_settlement_owner;
#else
                    will_try = true;
#endif
                    if (will_try)
                    {
                        break;
                    }
                    else
                    {
                        if (cannot_due_traits_and_relations_with_noble)
                            log($"{hero.Name} cannot_due_traits_and_relations_with_noble {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) persuade {(direction_to_revenge ? "to revenge" : "to be pasive")}");
                        if (cannot_due_traits_and_relations_with_settlement_owner)
                            log($"{hero.Name} cannot_due_traits_and_relations_with_settlement_owner {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) persuade {(direction_to_revenge ? "to revenge" : "to be pasive")}");
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
