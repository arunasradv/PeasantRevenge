using Helpers;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace PeasantRevenge
{
    internal class NotableWantRevengeIssueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this,new Action<Hero>(this.OnCheckForIssue));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private bool ConditionsHold(Hero issueGiver)
        {
            return issueGiver.CurrentSettlement!=null&&
                issueGiver.CurrentSettlement.IsVillage&&
                issueGiver.CurrentSettlement.IsUnderRaid&&
                issueGiver.IsRuralNotable&&
                issueGiver.GetTraitLevel(DefaultTraits.Mercy)<=0&&issueGiver.GetTraitLevel(DefaultTraits.Valor)>=0&&
                issueGiver.CurrentSettlement.Village.Bound.Town.Security<=99f&&
                issueGiver.CurrentSettlement.Village.Bound.BoundVillages.Any(delegate (Village x)
            {
                if(x.Settlement!=issueGiver.CurrentSettlement&&!x.Settlement.IsUnderRaid)
                {
                    return x.Settlement.Notables.Any((Hero notable) => notable.IsHeadman&&notable.CanHaveQuestsOrIssues());
                }
                return false;
            });
        }

        private void OnCheckForIssue(Hero hero)
        {
            if(this.ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(hero,new PotentialIssueData(
                    new PotentialIssueData.StartIssueDelegate(this.OnSelected),
                    typeof(NotableWantRevengeIssueBehavior.NotableWantRevengeIssue),
                    IssueBase.IssueFrequency.VeryCommon,null));
                return;
            }
            Campaign.Current.IssueManager.AddPotentialIssueData(hero,new PotentialIssueData(
                typeof(NotableWantRevengeIssueBehavior.NotableWantRevengeIssue),
                IssueBase.IssueFrequency.VeryCommon));
        }

        private IssueBase OnSelected(in PotentialIssueData pid,Hero issueOwner)
        {
            return new NotableWantRevengeIssue(issueOwner,CampaignTime.DaysFromNow(20f));
        }

        public class NotableWantRevengeIssue : IssueBase
        {
            [SaveableField(100)]
            private Settlement _targetSettlement; // The raided village
            [SaveableField(101)]
            private Hero _targetRaider; // Hero who raided the village
            [SaveableField(102)]
            private Hero _targetAccused; // Hero who should pay for the crime
            [SaveableField(103)]
            private int _rewardGold;
            [SaveableField(104)]
            private JournalLog _questProgressLogTest;
            [SaveableField(105)]
            private float _issueDuration;
            [SaveableField(106)]
            private float _questDuration;
            //[SaveableField(107)]
            //private Hero _targetPartyHero; // Party hero who has raider or accused hero as prissoner

            public NotableWantRevengeIssue(Hero issueOwner,CampaignTime issueDuration) : base(issueOwner,issueDuration)
            {
                _issueDuration=(float)issueDuration.ToDays;
            }

            protected override void AfterIssueCreation()
            {
                Village village = base.IssueOwner.CurrentSettlement.Village;
                this._targetSettlement=(village!=null) ? village.Settlement : null;
                this._targetRaider=this._targetSettlement.LastAttackerParty?.LeaderHero;
                this._targetAccused=this._targetSettlement.LastAttackerParty?.LeaderHero;             
                this._rewardGold=200;
            }

            public override TextObject IssueBriefByIssueGiver
            {
                get
                {
                    //TODO: add variations what are dependant on village damage level, notable traits, relations with player, kingdom...
                    TextObject textObject = new TextObject("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?} I have a problem. "+
                        "Our village has been raided. "+
                        "My {?PLAYER.GENDER}Lady{?}Lord{\\?} we have nothing to eat in our village.[if:convo_thinking][ib:grave]",null);
                    textObject.SetTextVariable("TARGET_SETTLEMENT",this._targetSettlement.Name);
                    return textObject;
                }
            }

            public override TextObject IssueAcceptByPlayer
            {
                get
                {
                    return new TextObject("{=a1n2zCaD}What exactly do you wish from me?",null);
                }
            }

            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get
                {
                    TextObject textObject;
                    //TODO: add variations what are dependant on village damage level, notable traits, relations with player, kingdom...
                    if(this._targetRaider!= null)
                    {
                        if(Hero.MainHero.PartyBelongedTo?.Party?.PrisonerHeroes?.Contains(this._targetRaider.CharacterObject)??false)
                        {
                            textObject = new TextObject("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?}one of your prisoner does look like the criminal, who raided our village. " +
                                                    "[if:convo_angry][ib:confident3]I'm asking you for fair justice.",null);
                        }
                        else
                        {
                            if(this._targetRaider.CharacterObject.IsPlayerCharacter)
                            {
                                textObject=new TextObject("{=*}It's you! I'll someday get revenge on you![if:convo_shocked][if:convo_astonished][if:convo_bared_teeth]",null);
                            }
                            else
                            {
                                textObject=new TextObject("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?} please find someone, who raided our village. "+
                                                        "[if:convo_angry][ib:confident3]I'm asking you for fair justice.",null);
                            }
                        }
                    }
                    else
                    {
                        textObject=new TextObject("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?} please find someone, who raided our village. "+
                                                    "[if:convo_angry][ib:confident3]I'm asking you for fair justice.",null);
                    }
                    
                    StringHelpers.SetCharacterProperties("PLAYER",Hero.MainHero.CharacterObject);
                    return textObject;
                }
            }

            public override TextObject IssueQuestSolutionAcceptByPlayer
            {
                get
                {
                    return new TextObject("{=*}All right.",null);
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

            public override TextObject Title
            {
                get
                {
                    TextObject textObject = new TextObject("{=*}{QUEST_GIVER.NAME} revenge on the raiders",null);
                    StringHelpers.SetCharacterProperties("QUEST_GIVER",base.IssueOwner.CharacterObject,textObject,false);
                    //StringHelpers.SetCharacterProperties("TARGET_HERO",this._targetRaider.CharacterObject,textObject,false);
                    return textObject;
                }
            }

            public override TextObject Description
            {
                get
                {
                    TextObject result = new TextObject("{=*}A landowner needs help to get revenge on the raiders",null);
                    StringHelpers.SetCharacterProperties("ISSUE_GIVER",base.IssueOwner.CharacterObject,null,false);
                    return result;
                }
            }
            public override IssueFrequency GetFrequency()
            {
                return IssueBase.IssueFrequency.Common;
            }

            public override bool IssueStayAliveConditions()
            {
                //if(!base.IssueOwner.CurrentSettlement.IsRaided)
                {
                    if(this._targetSettlement.Notables.Any((Hero x) => x.IsHeadman || x.IsNotable))
                    {
                        return true;
                    }
                }
                return false;
            }

            protected override bool CanPlayerTakeQuestConditions(Hero issueGiver,out PreconditionFlags flag,out Hero relationHero,out SkillObject skill)
            {
                skill=null;
                relationHero=null;
                flag=IssueBase.PreconditionFlags.None;
                if(issueGiver.GetRelationWithPlayer()<-99f)
                {
                    flag|=IssueBase.PreconditionFlags.Relation;
                    relationHero=issueGiver;
                }
                return flag==IssueBase.PreconditionFlags.None;
            }

            protected override void CompleteIssueWithTimedOutConsequences()
            {

            }

            protected override QuestBase GenerateIssueQuest(string questId)
            {
                return new NotableWantRevengeIssueBehavior.NotableWantRevengeIssueQuest(questId,base.IssueOwner,
                    CampaignTime.DaysFromNow(this._questDuration),this.RewardGold,this._targetSettlement,this._targetRaider,this._targetAccused);
            }

            protected override void HourlyTick()
            {
              
            }

            protected override void OnGameLoad()
            {
            }


        }

        public class NotableWantRevengeIssueQuest : QuestBase
        {
            [SaveableField(10)]
            private Settlement _targetSettlement; // The raided village
           
            [SaveableField(11)]            
            private Hero _targetRaider; // Hero who raided the village
            
            [SaveableField(12)]
            private Hero _targetAccused; // Hero who should pay for the crime

            [SaveableField(13)]
            private int _rewardGold;

            [SaveableField(14)]
            private JournalLog _startQuestLog;

            [SaveableField(16)]
            private float _questDuration;

            //[SaveableField(17)]
            //private Hero _targetPartyHero; // Party hero who has raider or accused hero as prissoner

            [SaveableField(18)]
            Hero IssueOwner;

            public NotableWantRevengeIssueQuest(string questId,Hero questGiver,CampaignTime duration,int rewardGold,
                Settlement targetSettlement,Hero targetRaider,Hero targetAccused) : base(questId,questGiver,duration,rewardGold)
            {
                this.IssueOwner=questGiver;
                this._targetSettlement=targetSettlement;
                this._targetRaider=targetRaider;
                this._targetAccused=targetAccused;
                this._rewardGold=rewardGold;
                this._questDuration=(float)duration.ToDays;
                base.AddTrackedObject(base.QuestGiver);
                this.SetDialogs();
                base.InitializeQuestOnCreation();
            }

            public override TextObject Title
            {
                get
                {
                    TextObject textObject = new TextObject("{=*}{QUEST_GIVER.NAME}'s revenge",null);
                    StringHelpers.SetCharacterProperties("QUEST_GIVER",base.QuestGiver.CharacterObject,textObject,false);                   
                    return textObject;
                }
            }

            public override bool IsRemainingTimeHidden
            {
                get
                {
                    return false;
                }
            }
            protected override void HourlyTick()
            {
                
            }

            protected override void InitializeQuestOnGameLoad()
            {
                this.SetDialogs();
            }

            //TODO: Finish setting dialogs
            private bool NotableDialogCondition()
            {
                return Hero.OneToOneConversationHero==base.QuestGiver;
            }

            private void QuestAcceptedConsequences()
            {
                base.StartQuest();
                TextObject PlayerStartsQuestLogText = 
                    new TextObject("{=*}{QUEST_GIVER.LINK} want to give justice to the raiders." +
                    " You have accepted to support peasant's demands.",null);                
                StringHelpers.SetCharacterProperties("QUEST_GIVER",base.QuestGiver.CharacterObject,PlayerStartsQuestLogText,false);                            
                this._startQuestLog = base.AddLog(PlayerStartsQuestLogText);

                //AddHeroToQuestLog(i);

                //this._checkForMissionEnd=true; //TODO: check if I need to use it.
            }

            // Conversation with Raider
           
            protected override void SetDialogs()
            {
                //Campaign.Current.ConversationManager.AddDialogFlow(this.FirstDialogFlow(),this);
                //Campaign.Current.ConversationManager.AddDialogFlow(this.SecondDialogFlow(),this);
                //Campaign.Current.ConversationManager.AddDialogFlow(this.FailedDialogFlow(),this);
                
                this.OfferDialogFlow=DialogFlow.CreateDialogFlow("issue_classic_quest_start",100).
                    NpcLine(new TextObject("{=*}I'm delighted.",null),null,null).
                    Condition(new ConversationSentence.OnConditionDelegate(this.NotableDialogCondition)).
                    Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences)).CloseDialog();
                
                this.DiscussDialogFlow=DialogFlow.CreateDialogFlow("quest_discuss",100).
                    NpcLine(new TextObject("{=*}How is my problem going?[if:convo_delighted][ib:hip]",null),null,null).
                    Condition(new ConversationSentence.OnConditionDelegate(this.NotableDialogCondition)).Consequence(delegate
                {
                    Campaign.Current.ConversationManager.ConversationEndOneShot+=MapEventHelper.OnConversationEnd; // just ending conversation here
                }).BeginPlayerOptions().PlayerOption(new TextObject("{=*}Yes, it is in a progress.",null),null).
                NpcLine(new TextObject("{=QsL6qcDb}That's very good to hear! Thank you.[if:convo_merry]",null),null,null).
                CloseDialog().PlayerOption(new TextObject("{=*}I need to check the evidence now actually.",null),null).
                NpcLine(new TextObject("{=*}Do not give {?TARGET_HERO.GENDER}her{?}him{\\?} some chance. I'm sure {?TARGET_HERO.GENDER}she{?}he{\\?} is guilty of this.",null),null,null).
                CloseDialog().EndPlayerOptions().CloseDialog();
            }
        }

        public class NotableWantRevengeIssueTypeDefiner : SaveableTypeDefiner
        {
            public NotableWantRevengeIssueTypeDefiner() : base(808269866)
            {
            }

            protected override void DefineClassTypes()
            {
                base.AddClassDefinition(typeof(NotableWantRevengeIssueBehavior.NotableWantRevengeIssue),1,null);
                base.AddClassDefinition(typeof(NotableWantRevengeIssueBehavior.NotableWantRevengeIssueQuest),2,null);
            }
        }
    }
}
