using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using static PeasantRevenge.Common;

namespace PeasantRevenge
{

    /// <summary>
    /// Issue for notable peasant in the village
    /// TODO: issue for town notable or governor.
    /// </summary>
    internal class NotableWantRevengeIssueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoadedEvent);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnGameLoadedEvent(CampaignGameStarter campaignGameStarter)
        {
            //TODO: Add 'start' dialogues for lords
        }
        // TODO: If raider is player, the quest should autostart, but now I do not know how to run multiple issues

        private bool ConditionsHold(Hero issueGiver)
        {
            if (issueGiver.HomeSettlement != null &&
                issueGiver.HomeSettlement.IsVillage &&
                issueGiver.HomeSettlement.LastAttackerParty != null &&
                issueGiver.IsRuralNotable &&
                !CfgParser.hero_trait_list_condition(issueGiver, _cfg.values.peasantRevengerExcludeTrait, out string parseerror) &&
                issueGiver.HomeSettlement.Village.Bound.Town.Security <= 99f)
            {
                log($"Revenge {issueGiver.Name} of {issueGiver.HomeSettlement.Name}");
                return true;
            }
            return false;
        }

        private void OnCheckForIssue(Hero hero)
        {
            if (this.ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(
                    hero,
                    new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(
                        this.OnStartIssue),
                        typeof(NotableWantRevengeIssue),
                    IssueBase.IssueFrequency.VeryCommon, null));

                if (hero.HomeSettlement.LastAttackerParty.LeaderHero != null)
                {
                    if (hero.HomeSettlement.LastAttackerParty.LeaderHero == Hero.MainHero)
                    {
                        hero.Issue.StartIssueWithQuest();
                        log($"Start Revenge {hero.Name} of {hero.HomeSettlement?.Name}");
                    }
                }
                return;
            }
            else
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(
                    hero,
                    new PotentialIssueData(typeof(NotableWantRevengeIssue),
                    IssueBase.IssueFrequency.VeryCommon));
            }
        }

        private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
        {
            return new NotableWantRevengeIssue(issueOwner);
        }

        public class NotableWantRevengeIssueTypeDefiner : SaveableTypeDefiner
        {
            public NotableWantRevengeIssueTypeDefiner() : base(808269866)
            {
            }

            protected override void DefineClassTypes()
            {
                base.AddClassDefinition(typeof(NotableWantRevengeIssue), 1, null);
                base.AddClassDefinition(typeof(NotableWantRevengeQuest), 2, null);
            }

            // protected override void DefineEnumTypes()
            // {
            //     AddEnumDefinition(typeof(Common.event_status), 3);
            // }
        }
    }
}
