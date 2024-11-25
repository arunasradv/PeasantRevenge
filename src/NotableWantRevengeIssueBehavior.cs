using Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;


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
            CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this,new Action<Hero>(this.OnCheckForIssue));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this,OnGameLoadedEvent);
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
            if(issueGiver.HomeSettlement!=null&&
                issueGiver.HomeSettlement.IsVillage&&
                (/*issueGiver.HomeSettlement.IsUnderRaid ||*/ issueGiver.HomeSettlement.LastAttackerParty!=null)&&
                issueGiver.IsRuralNotable&&
                issueGiver.GetTraitLevel(DefaultTraits.Mercy)<=0&&issueGiver.GetTraitLevel(DefaultTraits.Valor)>=0&&
                issueGiver.HomeSettlement.Village.Bound.Town.Security<=99f)
            {

                System.Diagnostics.Debug.WriteLine($"ConditionsHold for {issueGiver.Name.ToString()} of {issueGiver.HomeSettlement.Name.ToString()}");

                Village village = issueGiver.HomeSettlement.Village;
                return village!=null;
            }
            return false;
        }

        private void OnCheckForIssue(Hero hero)
        {
            if(this.ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(hero,new PotentialIssueData(
                    new PotentialIssueData.StartIssueDelegate(this.OnStartIssue),
                    typeof(NotableWantRevengeIssue),
                    IssueBase.IssueFrequency.VeryCommon,null));
                return;
            }
            //Campaign.Current.IssueManager.AddPotentialIssueData(hero,new PotentialIssueData(
            //    typeof(NotableWantRevengeIssueBehavior.NotableWantRevengeIssue),
            //    IssueBase.IssueFrequency.VeryCommon));
        }

        private IssueBase OnStartIssue(in PotentialIssueData pid,Hero issueOwner)
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
                base.AddClassDefinition(typeof(NotableWantRevengeIssue),1,null);
                base.AddClassDefinition(typeof(NotableWantRevengeIssueQuest),2,null);
            }
        }
    }
}
