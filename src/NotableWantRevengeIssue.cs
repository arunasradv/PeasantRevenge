using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace PeasantRevenge
{
    public class NotableWantRevengeIssue : IssueBase
    {
        [SaveableField(100)]
        private Settlement _targetSettlement; // The raided village
        [SaveableField(101)]
        private Hero _targetHero; // Hero who raided the village           
        [SaveableField(104)]
        private JournalLog _questProgressLogTest;

        //[SaveableField(107)]
        //private Hero _targetPartyHero; // Party hero who has raider or accused hero as prissoner

        public NotableWantRevengeIssue (Hero issueOwner) : base (issueOwner ,CampaignTime.DaysFromNow (100f))
        {
        }

        protected override void AfterIssueCreation ()
        {
            Village village = base.IssueOwner.HomeSettlement.Village;
            this._targetSettlement = (village != null) ? village.Settlement : null;
            this._targetHero = this._targetSettlement.LastAttackerParty?.LeaderHero;
        }

        public override TextObject IssueBriefByIssueGiver
        {
            get
            {
                //TODO: add variations what are dependant on village damage level, notable traits, relations with player, kingdom...
                TextObject textObject = new TextObject("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?} I have a problem. "+
                        "Our village has been raided. "+
                        "My {?PLAYER.GENDER}Lady{?}Lord{\\?} we have nothing to eat in our village.[if:convo_thinking][ib:grave]",null);
                textObject.SetTextVariable ("TARGET_SETTLEMENT" ,this._targetSettlement.Name);
                return textObject;
            }
        }

        public override TextObject IssueAcceptByPlayer
        {
            get
            {
                return new TextObject ("{=a1n2zCaD}What exactly do you wish from me?" ,null);
            }
        }

        public override TextObject IssueQuestSolutionExplanationByIssueGiver
        {
            get
            {
                TextObject textObject;
                //TODO: add variations what are dependant on village damage level, notable traits, relations with player, kingdom...
                if(this._targetHero != null)
                {
                    if(Hero.MainHero.PartyBelongedTo?.Party?.PrisonerHeroes?.Contains (this._targetHero.CharacterObject) ?? false)
                    {
                        textObject = new TextObject ("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?}one of your prisoner does look like the criminal, who raided our village. " +
                                                "[if:convo_angry][ib:confident3]I'm asking you for fair justice." ,null);
                    }
                    else
                    {
                        if(this._targetHero.CharacterObject.IsPlayerCharacter)
                        {
                            textObject = new TextObject ("{=*}It's you! I'll someday get revenge on you![if:convo_shocked][if:convo_astonished][if:convo_bared_teeth]" ,null);
                        }
                        else
                        {
                            textObject = new TextObject ("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?} please find, who raided our village. " +
                                                    "[if:convo_angry][ib:confident3]I'm asking you for fair justice." ,null);
                        }
                    }
                }
                else
                {
                    textObject = new TextObject ("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?} please find someone, who raided our village. " +
                                                "[if:convo_angry][ib:confident3]I'm asking you for fair justice." ,null);
                }

                StringHelpers.SetCharacterProperties ("PLAYER" ,Hero.MainHero.CharacterObject);
                return textObject;
            }
        }

        public override TextObject IssueQuestSolutionAcceptByPlayer
        {
            get
            {
                return new TextObject ("{=*}All right." ,null);
            }
        }

        public override bool IsThereAlternativeSolution
        {
            get
            {
                return false;
            }
        }

        public override bool IsThereLordSolution
        {
            get
            {
                return false;
            }
        }

        protected override bool IssueQuestCanBeDuplicated
        {
            get
            {
                return true;
            }
        }

        public override TextObject Title
        {
            get
            {
                TextObject textObject = new TextObject("{=*}{QUEST_GIVER.NAME} revenge on the raiders",null);
                StringHelpers.SetCharacterProperties ("QUEST_GIVER" ,base.IssueOwner.CharacterObject ,textObject ,false);
                //StringHelpers.SetCharacterProperties("TARGET_HERO",this._targetRaider.CharacterObject,textObject,false);
                return textObject;
            }
        }

        public override TextObject Description
        {
            get
            {
                TextObject result = new TextObject("{=*}A landowner needs help to get revenge on the raiders",null);
                StringHelpers.SetCharacterProperties ("ISSUE_GIVER" ,base.IssueOwner.CharacterObject ,null ,false);
                return result;
            }
        }
        public override IssueFrequency GetFrequency ()
        {
            return IssueBase.IssueFrequency.VeryCommon;
        }

        public override bool IssueStayAliveConditions ()
        {
            if(base.IssueOwner.IsAlive)
            {
                return true;
            }
            return false;
        }

        protected override bool CanPlayerTakeQuestConditions (Hero issueGiver ,out PreconditionFlags flag ,out Hero relationHero ,out SkillObject skill)
        {
            skill = null;
            relationHero = null;
            flag = IssueBase.PreconditionFlags.None;
            if(issueGiver.GetRelationWithPlayer ( ) < -99f)
            {
                flag |= IssueBase.PreconditionFlags.Relation;
                relationHero = issueGiver;
            }
            return flag == IssueBase.PreconditionFlags.None;
        }

        private void _disband_quest_giver_party ()
        {
            if(base.IssueOwner.PartyBelongedTo != null)
            {
                if(base.IssueOwner.PartyBelongedTo.MapEvent == null) // crash during battle update map event, if not checked
                {
                    DestroyPartyAction.ApplyForDisbanding (base.IssueOwner.PartyBelongedTo ,base.IssueOwner.HomeSettlement);
                }
            }
        }

        protected override void CompleteIssueWithTimedOutConsequences ()
        {
            _disband_quest_giver_party ( );
        }

        protected override QuestBase GenerateIssueQuest (string questId)
        {
            return new NotableWantRevengeIssueQuest (questId ,base.IssueOwner ,
                CampaignTime.DaysFromNow (100f) ,this.RewardGold ,this._targetSettlement ,this._targetHero);
        }

        protected override void HourlyTick ()
        {

        }

        protected override void OnGameLoad ()
        {
        }
    }
}
