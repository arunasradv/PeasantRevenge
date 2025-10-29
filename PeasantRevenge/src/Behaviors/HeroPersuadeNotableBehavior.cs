using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
            //CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTickEvent);

        }

        private void DailyTickEvent()
        {
            //TestHeroPersuadeTheNotable();
            TestHeroPersuadeNotableConditions();
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
            bool hero_tend_to_revenge = !CfgParser.hero_trait_list_condition(hero, _cfg.values.peasantRevengerExcludeTrait, out string parseerror);

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

                bool probability_condition = true; //check_probability(chance, (int)hero.Age);

                if (probability_condition)
                {

                    bool to_revenge = CheckOnlyTraitsConditions(hero, null, _cfg.values.ai.lordTraitsApprovePeasantsPower);
                    int reason = HeroWillTryToPersuadeTheNotable(hero, settlement, to_revenge, out Hero notable);
                    if (reason > 0)
                    {
                        if (notable != null)
                        {
                            event_status persuaded = event_status.none;
                            bool _teach = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableChooseTeachTraitsAndRelationsWithSettlementOwner);
                            bool _expel = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableChooseExpelTraitsAndRelationsWithSettlementOwner);
                            bool _kill = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableChooseExecuteTraitsAndRelationsWithSettlementOwner);
                            bool _bribe = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableChooseBribeTraitsAndRelationsWithSettlementOwner);

                            int goldNeeded = get_notable_bribe_amount(notable);
                            bool money_con = CanAffordToSpendMoney(hero, goldNeeded, _cfg.values.ai.lordPersuadeNotableWillAffordPartOfHisSavingsToPayForBribe);
                            bool will_accept_bribe = CheckConditions(notable, hero, _cfg.values.ai.notableWillAcceptTheBribe);
                            bool traits_allow = CheckOnlyTraitsConditions(notable, hero, _cfg.values.ai.notableWillAcceptTheBribe);

                            event_status persuade_status = event_status.none;
                            int task_index;
                            List<PeasantRevengeConfiguration.TraitAndValue> traits_values;
                            int option_index;

                            if (_teach)
                            {
                                TeachHeroTraits(notable, _cfg.values.peasantRevengerExcludeTrait, !to_revenge);
                                persuade_status = to_revenge ? event_status.teach_to_revenge : event_status.teach_to_not_revenge;
                                task_index = GetTaskIndexByPersuadeStatus(persuade_status);
                                option_index = GetOptionIndexByHeroTraits(hero, task_index);
                                traits_values = GetTraitsAndValuesByTaskAndOption(task_index, option_index);
                                OnLordUseTraitsAndValues(hero, traits_values);
                                log($"{hero.Name} persuaded {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) {(to_revenge ? "to revenge" : "to be pasive")}.");
                                persuaded = event_status.show_example_success;
                            }

                            if (_bribe && persuaded == event_status.none)
                            {
                                if (money_con && will_accept_bribe && traits_allow)
                                {
                                    TeachHeroTraits(notable, _cfg.values.peasantRevengerExcludeTrait, !to_revenge);
                                    GiveGoldAction.ApplyBetweenCharacters(hero, notable, goldNeeded, false);
                                    log($"{hero.Name} bribed {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) {(to_revenge ? "to revenge" : "to be pasive")}.");
                                    persuaded = event_status.bribe_success;
                                }
                                else if (!will_accept_bribe && traits_allow)
                                {
                                    GiveGoldAction.ApplyBetweenCharacters(hero, notable, goldNeeded, false);
                                    log($"{hero.Name} failed to bribe {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) {(to_revenge ? "to revenge" : "to be pasive")}.");
                                    persuaded = event_status.bribe_fail;
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

                            if (persuaded == event_status.none)
                            {
                                if (can_remove_notable_from_village(notable))
                                {
                                    if (_expel)
                                    {
                                        persuade_status = event_status.accusation;
                                        task_index = GetTaskIndexByPersuadeStatus(persuade_status);
                                        option_index = GetOptionIndexByHeroTraits(hero, task_index);
                                        traits_values = GetTraitsAndValuesByTaskAndOption(task_index, option_index);
                                        OnLordUseTraitsAndValues(hero, traits_values);
                                        log($"{hero.Name} expeled {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) {(to_revenge ? "to revenge" : "to be pasive")}.");
                                        KillCharacterAction.ApplyByRemove(notable, true, true);
                                        persuaded = event_status.expeled;
                                    }

                                    if (_kill && persuaded == event_status.none)
                                    {
                                        OnLordExecuteRevengerAfterOrBeforeQuest(hero);
                                        OnHeroChopNotableHeadConsequence(hero, notable);
                                        log($"{hero.Name} killed {notable.Name} ({(hero.MapFaction.IsAtWarWith(notable.MapFaction) ? "enemy" : "ally")}) while seeking {(to_revenge ? "to revenge" : "to be pasive")}.");
                                        KillCharacterAction.ApplyByRemove(notable, true, true);
                                        persuaded = event_status.killed;
                                    }

                                    if (persuaded == event_status.none)
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

        private int HeroWillTryToPersuadeTheNotable(Hero hero, Settlement settlement, bool direction_to_revenge, out Hero notable)
        {
            int will_try = 0;

            notable = null;

            for (int i = 0; i < settlement.Notables.Count; i++)
            {
                notable = settlement.Notables.ElementAt(i);

                // if (notable == null || notable.Issue != null)
                // {
                //     continue;
                // }

                bool notable_can_revenge = !CfgParser.hero_trait_list_condition(notable, _cfg.values.peasantRevengerExcludeTrait, out string parseerror);

                bool notable_is_oposite = notable_can_revenge != direction_to_revenge;

                if (notable_is_oposite) // it means notable peasant is doing the opposite to hero preferred circumstances.
                {

                    bool cannot_due_traits_and_relations_with_noble = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableExcludeTraitsAndRelationsWithNotable); // lord cannot persuade notable in any way due to his traits and relations
                    bool cannot_due_traits_and_relations_with_settlement_owner = CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableExcludeTraitsAndRelationsWithSettlementOwner); // lord cannot persuade notable in any way due to his traits and relations

                    bool at_war = hero.MapFaction.IsAtWarWith(notable.MapFaction);
                    bool same_faction = hero.MapFaction == notable.MapFaction;
                    bool same_clan = hero.Clan == notable.Clan;
                    bool approve_revenge = CheckConditions(hero, notable, _cfg.values.ai.lordTraitsApprovePeasantsPower);
                    bool oppose_revenge = CheckConditions(hero, notable, _cfg.values.ai.lordTraitsOpposingPeasantsPower);

                    if (approve_revenge == false && oppose_revenge == false)
                    {
                        //choose random or stick to the quest task
                    }

                    if (direction_to_revenge)
                    {
                        will_try |= (approve_revenge && (same_faction || same_clan)) ? 1 : 0;
                    }
                    else
                    {
                        will_try |= (oppose_revenge && (same_faction || same_clan)) ? 16 : 0;
                        will_try |= (at_war || !(same_faction || same_clan)) ? 2 : 0;
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

                    if (cannot_due_traits_and_relations_with_noble && cannot_due_traits_and_relations_with_settlement_owner)
                    {
                        will_try = 0;
                    }
                    else
                    {
                        will_try |= (!cannot_due_traits_and_relations_with_noble ? 4 : 0) | (!cannot_due_traits_and_relations_with_settlement_owner ? 8 : 0);
                    }


                    if (will_try > 0)
                    {
                        break;
                    }
                }
            }
            return will_try;
        }

        private void TestHeroPersuadeNotableConditions()
        {
            int approve_revenge_count = 0;
            int oppose_revenge_count = 0;
            int prefered_to_revenge_count = 0;
            int cannot_due_traits_and_relations_with_noble_count = 0;
            int cannot_due_traits_and_relations_with_settlement_owner_count = 0;
            int notable_is_oposite_count = 0;

            log($"prefered_to_revenge_count\tnotable_is_oposite_count\tcannot_due_traits_and_relations_with_noble_count\tcannot_due_traits_and_relations_with_settlement_owner_count\tapprove_revenge_count\toppose_revenge_count");

            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero.IsLord && !hero.IsHumanPlayerCharacter)
                {
                    foreach (Settlement settlement in Settlement.All)
                    {
                        if (settlement.IsVillage && settlement.Notables != null && settlement.Notables.Count > 0)
                        {
                            for (int i = 0; i < settlement.Notables.Count; i++)
                            {
                                Hero notable = settlement.Notables.ElementAt(i);
                                bool direction_to_revenge = CheckOnlyTraitsConditions(hero, null, _cfg.values.ai.lordTraitsApprovePeasantsPower);
                                prefered_to_revenge_count += direction_to_revenge ? 1 : 0;
                                notable_is_oposite_count += (!CfgParser.hero_trait_list_condition(notable, _cfg.values.peasantRevengerExcludeTrait, out string parseerror) != direction_to_revenge) ? 1 : 0;
                                cannot_due_traits_and_relations_with_noble_count += CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableExcludeTraitsAndRelationsWithNotable) ? 1 : 0; // lord cannot persuade notable in any way due to his traits and relations
                                cannot_due_traits_and_relations_with_settlement_owner_count += CheckConditions(hero, notable, _cfg.values.ai.lordPersuadeNotableExcludeTraitsAndRelationsWithSettlementOwner) ? 1 : 0; // lord cannot persuade notable in any way due to his traits and relations
                                approve_revenge_count += CheckConditions(hero, notable, _cfg.values.ai.lordTraitsApprovePeasantsPower) ? 1 : 0;
                                oppose_revenge_count += CheckConditions(hero, notable, _cfg.values.ai.lordTraitsOpposingPeasantsPower) ? 1 : 0;
                            }

                        }
                    }


                    log($"{hero.Name}\t{prefered_to_revenge_count}\t{notable_is_oposite_count}\t{cannot_due_traits_and_relations_with_noble_count}\t{cannot_due_traits_and_relations_with_settlement_owner_count}\t{approve_revenge_count}\t{oppose_revenge_count}");


                    notable_is_oposite_count = 0;
                    cannot_due_traits_and_relations_with_noble_count = 0;
                    cannot_due_traits_and_relations_with_settlement_owner_count = 0;
                    prefered_to_revenge_count = 0;
                    approve_revenge_count = 0;
                    oppose_revenge_count = 0;

                }
            }
        }

        private void TestHeroPersuadeTheNotable()
        {
            int hero_persuade_to_revenge_count = 0;
            int hero_persuade_to_not_revenge_count = 0;

            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero.IsLord && !hero.IsHumanPlayerCharacter)
                {
                    foreach (Settlement settlement in Settlement.All)
                    {
                        if (settlement.IsVillage && settlement.Notables != null && settlement.Notables.Count > 0)
                        {
                            bool to_revenge = CheckOnlyTraitsConditions(hero, null, _cfg.values.ai.lordTraitsApprovePeasantsPower);
                            if (HeroWillTryToPersuadeTheNotable(hero, settlement, to_revenge, out Hero notable) > 0)
                            {
                                if (notable != null)
                                {
                                    hero_persuade_to_revenge_count += to_revenge ? 1 : 0;
                                    hero_persuade_to_not_revenge_count += !to_revenge ? 1 : 0;


                                }
                            }
                        }
                    }
                    if (hero_persuade_to_revenge_count > 0)
                    {
                        log($"{hero.Name} RE {hero_persuade_to_revenge_count} vilages.");
                    }
                    if (hero_persuade_to_not_revenge_count > 0)
                    {
                        log($"{hero.Name} NR {hero_persuade_to_not_revenge_count} vilages.");
                    }
                    hero_persuade_to_revenge_count = 0;
                    hero_persuade_to_not_revenge_count = 0;
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
