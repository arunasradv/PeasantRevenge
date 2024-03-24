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
                issueGiver.GetTraitLevel(DefaultTraits.Mercy)<=0&&issueGiver.GetTraitLevel(DefaultTraits.Generosity)<=0&&
                issueGiver.CurrentSettlement.Village.Bound.Town.Security<=70f&&
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
            return new NotableWantRevengeIssue(issueOwner,CampaignTime.Days(15f));
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
                this._targetRaider=this._targetSettlement.LastAttackerParty.LeaderHero;
                this._targetAccused=this._targetSettlement.LastAttackerParty.LeaderHero;
                //this._issueDuration = 30f;
                this._questDuration=20f;
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
                    //TODO: add variations what are dependant on village damage level, notable traits, relations with player, kingdom...
                    TextObject textObject = new TextObject("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?} your prisoner does look like the criminal,"+
                        " who raided our village. "+
                        "[if:convo_angry][ib:confident3]My {?PLAYER.GENDER}Lady{?}Lord{\\?} I'm asking you for fair justice.",null);
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
                    TextObject textObject = new TextObject("{=*}{QUEST_GIVER.NAME} do revenge against {TARGET_HERO.NAME}",null);
                    StringHelpers.SetCharacterProperties("QUEST_GIVER",base.IssueOwner.CharacterObject,textObject,false);
                    StringHelpers.SetCharacterProperties("TARGET_HERO",this._targetRaider.CharacterObject,textObject,false);
                    return textObject;
                }
            }

            public override TextObject Description => throw new NotImplementedException();

            public override IssueFrequency GetFrequency()
            {
                return IssueBase.IssueFrequency.Common;
            }

            public override bool IssueStayAliveConditions()
            {
                if(!base.IssueOwner.CurrentSettlement.IsRaided)
                {
                    if(this._targetSettlement.Notables.Any((Hero x) => x.IsHeadman))
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
                    CampaignTime.Days(this._questDuration),this.RewardGold,this._targetSettlement,this._targetRaider,this._targetAccused);
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
            private JournalLog _questProgressLogTest;

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
                    TextObject textObject = new TextObject("{=*}{QUEST_GIVER.NAME} do revenge against {TARGET_HERO.NAME}",null);
                    StringHelpers.SetCharacterProperties("QUEST_GIVER",this.IssueOwner.CharacterObject,textObject,false);
                    StringHelpers.SetCharacterProperties("TARGET_HERO",this._targetRaider.CharacterObject,textObject,false);
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
                throw new NotImplementedException();
            }

            protected override void InitializeQuestOnGameLoad()
            {
                this.SetDialogs();
            }

            //TODO: Finish setting dialogs
            private DialogFlow FirstDialogFlow()
            {
                return DialogFlow.CreateDialogFlow("start",125).NpcLine(new TextObject("{=XH66Leg5}Greetings, my {?PLAYER.GENDER}lady{?}lord{\\?}. I have heard much of your deeds. Thank you for agreeing to train me. I hope I won't disappoint you.[if:convo_grateful][ib:demure]",null),null,null).Condition(() => Hero.OneToOneConversationHero==this._youngHero&&!this._firstConversationInitialized).Consequence(delegate
                {
                    Campaign.Current.ConversationManager.ConversationEndOneShot+=this.FirstConversationEndConsequence;
                }).BeginPlayerOptions().PlayerOption(new TextObject("{=oJJiKTSL}You are welcome.",null),null).NpcLine(new TextObject("{=wlKtDR2z}Thank you, {?PLAYER.GENDER}my lady{?}sir{\\?}.",null),null,null).CloseDialog().PlayerOption(new TextObject("{=FHeJ8bsX}We will see about that.",null),null).NpcLine(new TextObject("{=kc3RfwFb}I'll try to be useful to you, {?PLAYER.GENDER}my lady{?}sir{\\?}.",null),null,null).EndPlayerOptions().PlayerLine(new TextObject("{=kJwpbptU}Well, try to stay close to me at all times and try to learn as much as you can.",null),null).NpcLine(new TextObject("{=EaifHOao}Yes, {?PLAYER.GENDER}my lady{?}sir{\\?}, I will.",null),null,null).CloseDialog();
            }

            private DialogFlow SecondDialogFlow()
            {
                return DialogFlow.CreateDialogFlow("start",125).BeginNpcOptions().NpcOption(new TextObject("{=APEBfqyW}Greetings my {?PLAYER.GENDER}lady{?}lord{\\?}. Do you wish something from me?[if:convo_innocent_smile][ib:normal2]",null),new ConversationSentence.OnConditionDelegate(this.default_conversation_with_young_hero_condition),null,null).BeginPlayerOptions().PlayerOption(new TextObject("{=BO0f1Klt}So - how do you find life in our company? Is it all you expected?.",null),null).NpcLine(new TextObject("{=e3e79n9B}It is all I expected and more, captain. I am glad that you took me with you.[if:convo_grateful][ib:normal2]",null),null,null).PlayerLine(new TextObject("{=dbG3PGXL}I'm glad you think that way. Combat aside, have you learned anything special?",null),null).NpcLine(new TextObject("{=8L9W34D6}{NPC_EXPERIENCE_LINE}",null),null,null).Condition(new ConversationSentence.OnConditionDelegate(this.npc_experience_line_condition)).PlayerLine(new TextObject("{=Rh0DlvvE}I'm glad you see it that way. Go on. Continue your training.",null),null).NpcLine(new TextObject("{=dnvPDnzS}I will, my {?PLAYER.GENDER}lady{?}lord{\\?}. Thank you[if:convo_grateful][ib:demure]",null),null,null).CloseDialog().PlayerOption(new TextObject("{=Lk6ln3sR}We seem to have got separated but I have found you. Join me, as we need to continue your training.",null),null).Condition(new ConversationSentence.OnConditionDelegate(this.PupilJoinMeCondition)).NpcLine(new TextObject("{=0coOJAvg}Yes, {?PLAYER.GENDER}madam{?}sir{\\?}. Thank you.",null),null,null).Consequence(delegate
                {
                    MobileParty.MainParty.MemberRoster.AddToCounts(this._youngHero.CharacterObject,1,false,0,0,true,-1);
                }).CloseDialog().EndPlayerOptions().NpcOption(new TextObject("{=kUbovNbE}My {?PLAYER.GENDER}lady{?}lord{\\?}. The agreed training time with you is over. I thank you for everything. It's been a very productive for me.[if:convo_delighted][ib:demure]",null),new ConversationSentence.OnConditionDelegate(this.quest_finished_conversation_with_young_hero_condition),null,null).PlayerLine(new TextObject("{=bS0bBgp3}I'm happy to hear this. Tell me, what is the most important lesson you've learned from me?",null),null).NpcLine(new TextObject("{=8L9W34D6}{NPC_EXPERIENCE_LINE}",null),null,null).Condition(new ConversationSentence.OnConditionDelegate(this.npc_experience_line_condition)).PlayerLine(new TextObject("{=orprhyYl}I'm glad you see it that way. Very well then, off you go. Send my regards to your family. I hope to see you again one day. I am sure you will make an excellent commander.",null),null).NpcLine(new TextObject("{=IBXfCLMp}I certainly hope too {?PLAYER.GENDER}lady{?}lord{\\?}! Again, I want to thank you for everything, before I go, please accept this gift as a humble gratitude.[if:convo_calm_friendly]",null),null,null).Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestCompletedWithSuccessAfterConversation)).CloseDialog().EndNpcOptions();
            }

            private DialogFlow FailedDialogFlow()
            {
                return DialogFlow.CreateDialogFlow("start",125).NpcLine(new TextObject("{=vbbc6sIU}I regret to tell you that my progress under your tutelage is not satisfactory. I should return to my clan to resume my studies. Thank you for your trouble anyway.",null),null,null).Condition(() => Hero.OneToOneConversationHero==this._youngHero&&this._showQuestFailedConversation).CloseDialog();
            }

            protected override void SetDialogs()
            {
                Campaign.Current.ConversationManager.AddDialogFlow(this.FirstDialogFlow(),this);
                Campaign.Current.ConversationManager.AddDialogFlow(this.SecondDialogFlow(),this);
                Campaign.Current.ConversationManager.AddDialogFlow(this.FailedDialogFlow(),this);
                
                this.OfferDialogFlow=DialogFlow.CreateDialogFlow("issue_classic_quest_start",100).
                    NpcLine(new TextObject("{=*}He'll be delighted. I'll tell him to join you as soon as possible.",null),null,null).
                    Condition(new ConversationSentence.OnConditionDelegate(this.NotableDialogCondition)).
                    Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences)).CloseDialog();
                
                this.DiscussDialogFlow=DialogFlow.CreateDialogFlow("quest_discuss",100).
                    NpcLine(new TextObject("{=*}How is the training going? Are you happy with your student?[if:convo_delighted][ib:hip]",null),null,null).
                    Condition(new ConversationSentence.OnConditionDelegate(this.NotableDialogCondition)).Consequence(delegate
                {
                    Campaign.Current.ConversationManager.ConversationEndOneShot+=MapEventHelper.OnConversationEnd;
                }).BeginPlayerOptions().PlayerOption(new TextObject("{=*}Yes, he is a promising boy.",null),null).
                NpcLine(new TextObject("{=QsL6qcDb}That's very good to hear! Thank you.[if:convo_merry]",null),null,null).
                CloseDialog().PlayerOption(new TextObject("{=SbbAhpTu}He is yet to prove himself actually.",null),null).
                NpcLine(new TextObject("{=aHid0t6n}Give him some chance I'm sure he will prove himself soon.",null),null,null).
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
