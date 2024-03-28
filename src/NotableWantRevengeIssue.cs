using Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Issues;
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
                    typeof(NotableWantRevengeIssueBehavior.NotableWantRevengeIssue),
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

        public static bool HeroIsPlayersPrisoner(Hero hero)
        {
            if(hero==null)
                return false;
            return (Hero.MainHero.PartyBelongedTo?.Party?.PrisonerHeroes?.Contains(hero.CharacterObject)??false);
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

            //[SaveableField(107)]
            //private Hero _targetPartyHero; // Party hero who has raider or accused hero as prissoner

            public NotableWantRevengeIssue(Hero issueOwner) : base(issueOwner,CampaignTime.DaysFromNow(10f))
            {
            }

            protected override void AfterIssueCreation()
            {
                Village village = base.IssueOwner.HomeSettlement.Village;
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
                    if(this._targetRaider!=null)
                    {
                        if(Hero.MainHero.PartyBelongedTo?.Party?.PrisonerHeroes?.Contains(this._targetRaider.CharacterObject)??false)
                        {
                            textObject=new TextObject("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?}one of your prisoner does look like the criminal, who raided our village. "+
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
                                textObject=new TextObject("{=*}My {?PLAYER.GENDER}Lady{?}Lord{\\?} please find, who raided our village. "+
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
                return IssueBase.IssueFrequency.VeryCommon;
            }

            public override bool IssueStayAliveConditions()
            {
                if(base.IssueOwner.IsAlive)
                {
                    return true;
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
                if(base.IssueOwner.PartyBelongedTo!=null)
                {
                    if(base.IssueOwner.PartyBelongedTo.MapEvent==null) // crash during battle update map event, if not checked
                    {
                        DestroyPartyAction.ApplyForDisbanding(base.IssueOwner.PartyBelongedTo,base.IssueOwner.HomeSettlement); // will set clear flag in events
                    }
                }
            }

            protected override QuestBase GenerateIssueQuest(string questId)
            {
                return new NotableWantRevengeIssueBehavior.NotableWantRevengeIssueQuest(questId,base.IssueOwner,
                    CampaignTime.DaysFromNow(10f),this.RewardGold,this._targetSettlement,this._targetRaider,this._targetAccused);
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
            private JournalLog _startQuestLog;

            [SaveableField(14)]
            private CampaignTime _questGiverTravelStart;


            public NotableWantRevengeIssueQuest(string questId,Hero questGiver,CampaignTime duration,int rewardGold,
                Settlement targetSettlement,Hero targetRaider,Hero targetAccused) : base(questId,questGiver,duration,rewardGold)
            {
                this._targetSettlement=targetSettlement;
                this._targetRaider=targetRaider;
                this._targetAccused=targetAccused;
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
                RevengerTravelProgress();
            }

            protected override void InitializeQuestOnGameLoad()
            {
                this.SetDialogs();
            }

            protected override void RegisterEvents()
            {
                CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this,HeroPrisonerTaken);
                CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this,HeroPrisonerReleased);
                CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this,VillageBeingRaided);
                //TODO: Add check for player, if player is waitting in settlement, revenger should try to ambush the player... 
            }

            private void VillageBeingRaided(Village village)
            {
                //if(this._targetSettlement.Village.Bound.BoundVillages.Any((x)=>(x.StringId.Equals(village.StringId))))
                //    return;

                if(village.Settlement.LastAttackerParty==null||village.Settlement.LastAttackerParty.Party.LeaderHero==null)
                    return;

                if(this._targetRaider==null) //TODO: add list of raiders , so quest could be updated with new raiders.
                {
                    this._targetRaider=village.Settlement.LastAttackerParty.Party.LeaderHero;
                    AddHeroToQuestLog(this._targetRaider);
                }
            }

            private void HeroPrisonerReleased(Hero prisoner,PartyBase party,IFaction faction,EndCaptivityDetail detail)
            {
                UpdateJournalProgress(prisoner,0);
                RevengerStopTraveling();
            }

            private void HeroPrisonerTaken(PartyBase party,Hero prisoner)
            {
                if(UpdateJournalProgress(prisoner,1))
                {
                    RevengerTravelStartCounter();
                }
            }

            protected TextObject IssueOwnerTravelStartLogText
            {
                get
                {
                    return new TextObject("{=*}Revenger will learn about raiders capture soon.",null);
                }
            }
            protected TextObject IssueOwnerTravelingLogText
            {
                get
                {
                    return new TextObject("{=*}Revenger is traveling.",null);
                }
            }

            private void RevengerTravelStartCounter()
            {
                _questGiverTravelStart=CampaignTime.HoursFromNow(10f); // TODO: Make travel start day dependant on how far raider has been captured from settlement.                
                base.AddLog(IssueOwnerTravelStartLogText);
            }

            private void RevengerStopTraveling()
            {
                if(base.QuestGiver.PartyBelongedTo!=null)
                {
                    base.QuestGiver.PartyBelongedTo.Ai.SetMoveGoToSettlement(_targetSettlement);
                }
            }

            private void RevengerTravelProgress()
            {
                if(_questGiverTravelStart.IsPast)
                {
                    if(base.QuestGiver.PartyBelongedTo == null &&
                        ((this._targetRaider != null && this._targetRaider.IsPrisoner) ||
                        (this._targetAccused!=null&&this._targetAccused.IsPrisoner)))
                    {
                        CreateNotableParty();

                        if(this._targetRaider!=null&&this._targetRaider.PartyBelongedToAsPrisoner!=null)
                        {
                            base.QuestGiver.PartyBelongedTo.Ai.SetMoveEscortParty(this._targetRaider.PartyBelongedToAsPrisoner.MobileParty);
                        }
                        else if(this._targetAccused!=null&&this._targetAccused.PartyBelongedToAsPrisoner!=null)
                        {
                            base.QuestGiver.PartyBelongedTo.Ai.SetMoveEscortParty(this._targetAccused.PartyBelongedToAsPrisoner.MobileParty);
                        }
                        base.AddLog(IssueOwnerTravelingLogText);
                    }
                    else if(base.QuestGiver.PartyBelongedTo!=null)
                    {
                        if(this._targetRaider!=null&&this._targetRaider.PartyBelongedToAsPrisoner!=null)
                        {
                            RevengerPartyMoveNearTarget(this._targetRaider.PartyBelongedToAsPrisoner.MobileParty);
                        }
                        else if(this._targetAccused!=null&&this._targetAccused.PartyBelongedToAsPrisoner!=null)
                        {
                            RevengerPartyMoveNearTarget(this._targetAccused.PartyBelongedToAsPrisoner.MobileParty);
                        }
                    }
                }
            }

            private void RevengerPartyMoveNearTarget(MobileParty mobileParty)
            {
                if(mobileParty!=null &&base.QuestGiver.PartyBelongedTo!=null)
                {
                    float peasantRevengePartyWaitLordDistance = 1f;
                    Vec2 pposition = mobileParty.Position2D;
                    Vec2 rvec2 = new Vec2(peasantRevengePartyWaitLordDistance>=0.0f ? peasantRevengePartyWaitLordDistance : 1.0f,0f);
                    rvec2.RotateCCW(MBRandom.RandomFloatRanged(6.28f));
                    base.QuestGiver.PartyBelongedTo.Ai.SetMoveGoToPoint(pposition+rvec2);
                }
            }

            private MobileParty CreateNotableParty()
            {
                string revengerPartyNameStart = "Revenger_";
                int peasantRevengeMaxPartySize = 20;
                float _ignoreForHours = 6f;
                int size = (int)base.QuestGiver.HomeSettlement.Village.Hearth>=peasantRevengeMaxPartySize-1 ? peasantRevengeMaxPartySize-1 : (int)base.QuestGiver.HomeSettlement.Village.Hearth;
                MobileParty mobileParty = MobileParty.CreateParty($"{revengerPartyNameStart}{base.QuestGiver.Name}".Replace(' ','_'),null,null);
                CharacterObject villager = base.QuestGiver.CharacterObject.Culture.Villager;
                TroopRoster troopRoster = new TroopRoster(mobileParty.Party);
                TextObject textObject = new TextObject("{=PRev0085}Revenger",null);
                troopRoster.AddToCounts(base.QuestGiver.CharacterObject,1,true,0,0,true,-1);
                troopRoster.AddToCounts(villager,size,false,0,0,true,-1);
                mobileParty.InitializeMobilePartyAtPosition(troopRoster,new TroopRoster(mobileParty.Party),base.QuestGiver.HomeSettlement.Position2D);
                mobileParty.InitializePartyTrade(200);
                mobileParty.SetCustomName(textObject);
                mobileParty.SetCustomHomeSettlement(base.QuestGiver.HomeSettlement);
                mobileParty.SetPartyUsedByQuest(true);
                mobileParty.ShouldJoinPlayerBattles=false;
                mobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"),size);
                mobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("butter"),size);
                mobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("cheese"),size);
                mobileParty.IgnoreForHours(_ignoreForHours);
                mobileParty.Ai.SetDoNotMakeNewDecisions(true);
                mobileParty.Party.SetVisualAsDirty();
                mobileParty.Aggressiveness=0f;
                return mobileParty;
            }

            //TODO: Finish setting dialogs
            private bool NotableDialogCondition()
            {
                return Hero.OneToOneConversationHero==base.QuestGiver;
            }

            private void QuestAcceptedConsequences()
            {
                base.StartQuest();

                TextObject textObject;
                //TODO: add variations what are dependant on village damage level, notable traits, relations with player, kingdom...
                if(this._targetRaider!=null)
                {
                    if(HeroIsPlayersPrisoner(this._targetRaider)) /*PLAYER HAS RAIDER AS PRISONER*/
                    {
                        textObject=new TextObject("{=*}You have captured {TARGET_HERO.NAME}, who has been raiding {TARGET_SETTLEMENT} village.",null);
                    }
                    else
                    {
                        if(this._targetRaider.CharacterObject.IsPlayerCharacter) /*PLAYER IS THE RAIDER*/
                        {
                            textObject=new TextObject("{=*}{QUEST_GIVER.LINK} said {?QUEST_GIVER.GENDER}she{?}he{\\?} will get revenge on you someday!",null);
                        }
                        else  /*PLAYER DOES NOT HAVE THE RAIDER AS PRISONER*/
                        {
                            textObject=new TextObject("{=*}{QUEST_GIVER.LINK} asked you to capture {TARGET_HERO.NAME}, who has been raiding the {TARGET_SETTLEMENT} village recently.",null);
                        }
                    }
                }
                else /*RAIDER IS NOT KNOWN*/
                {
                    textObject=new TextObject("{=*}{QUEST_GIVER.LINK} want to give justice to the raiders, who are raiding the {TARGET_SETTLEMENT} village.",null);
                }

                textObject.SetTextVariable("TARGET_SETTLEMENT",this._targetSettlement.Name);
                if(this._targetRaider!=null)
                {
                    StringHelpers.SetCharacterProperties("TARGET_HERO",this._targetRaider.CharacterObject,textObject,false);
                }
                StringHelpers.SetCharacterProperties("QUEST_GIVER",base.QuestGiver.CharacterObject,textObject,false);
                StringHelpers.SetCharacterProperties("PLAYER",Hero.MainHero.CharacterObject,textObject,false);

                this._startQuestLog=base.AddLog(textObject);

                AddHeroToQuestLog(this._targetRaider);

                if(this._targetRaider!=this._targetAccused)
                {
                    AddHeroToQuestLog(this._targetAccused);
                }

                if(HeroIsPlayersPrisoner(this._targetRaider))
                    UpdateJournalProgress(this._targetRaider,1);
                if(HeroIsPlayersPrisoner(this._targetAccused))
                    UpdateJournalProgress(this._targetAccused,1);

                //this._checkForMissionEnd=true; //TODO: check if I need to use it.
            }

            private void AddHeroToQuestLog(Hero hero)
            {
                if(hero==null)
                    return;

                string text = "{=*}{TARGET_HERO.LINK} of {CLAN}";
                TextObject textObject = new TextObject(text,null);
                StringHelpers.SetCharacterProperties("TARGET_HERO",hero.CharacterObject,textObject,false);
                textObject.SetTextVariable("CLAN",hero.Clan?.EncyclopediaLinkWithName);
                string taskText = "{=*}{NAME}";
                TextObject taskTextObject = new TextObject(taskText,null);
                StringHelpers.SetCharacterProperties("NAME",hero.CharacterObject,taskTextObject,false);

                if(base.JournalEntries.Where((x) => (x.TaskName?.ToString().Equals(taskTextObject.ToString())??false)).Count()==0)
                {
                    this._startQuestLog=base.AddDiscreteLog(textObject,taskTextObject,0,1,null,false);
                }
                //if(this._targetRaider.CharacterObject.IsPlayerCharacter)
                //{
                //    UpdateJournalProgress(hero.CharacterObject.Name.ToString(),1);
                //}
            }
                        
            /// <summary>
            /// Updates hero task status in quest.
            /// </summary>
            /// <param name="hero"></param>
            /// <param name="status"></param>
            /// <returns>true, if status was updated or created</returns>
            private bool UpdateJournalProgress(Hero hero,int status)
            {
                if(hero==this._targetRaider||hero==this._targetAccused)
                {
                    UpdateJournalDirectionsProgress(null,hero.Name.ToString(),status);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="text"></param>
            /// <param name="taskName"></param>
            /// <param name="status"></param>
            private void UpdateJournalDirectionsProgress(string text, string taskName,int status)
            {
                if(base.JournalEntries.Where((x) => (x.TaskName?.ToString().Equals(taskName)??false)).Count()==0 && text != null)
                {
                    this._startQuestLog=base.AddDiscreteLog(
                        new TextObject(text),//TODO: When Player is captured text should be like: Revenger will findout about your capture at about 1208 Summer 7; and next: Revenger is comming for you... 
                        new TextObject(taskName),status,1,null,false);
                }
                else
                {
                    UpdateTaskProgress(taskName,status); // if in case quest already has this direction task, just update 
                }                
            }

            private void UpdateTaskProgress(string taskName,int status)
            {
                int journalLogIndex = base.JournalEntries.FindIndex((x) => (x.TaskName!=null ? x.TaskName.ToString()==taskName.ToString() : false));
                if(journalLogIndex>=0&&journalLogIndex<base.JournalEntries.Count)
                {
                    base.JournalEntries [journalLogIndex].UpdateCurrentProgress(status);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Task name: {taskName} was not found and not updated to status {status}.");
                }
            }

 

        

            // Conversation with Raider

            protected override void SetDialogs()
            {
                Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDiscussReparationDialogFlow(),this);
                Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDiscussFateOfRaidersDialogFlow(),this);
                //Campaign.Current.ConversationManager.AddDialogFlow(this.FailedDialogFlow(),this);

                this.OfferDialogFlow=DialogFlow.CreateDialogFlow("issue_classic_quest_start",100).
                    NpcLine(new TextObject("{=*}I'm delighted.",null),null,null). //TODO: Change text for player is raider case!
                    Condition(new ConversationSentence.OnConditionDelegate(this.NotableDialogCondition)).
                    Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences)).CloseDialog();

                this.DiscussDialogFlow=DialogFlow.CreateDialogFlow("quest_discuss",100).
                    NpcLine(new TextObject("{=*}How are you dealing with my problem?[if:convo_delighted][ib:hip]",null),null,null).
                    Condition(new ConversationSentence.OnConditionDelegate(this.NotableDialogCondition)).
                    Consequence(delegate
                {
                    Campaign.Current.ConversationManager.ConversationEndOneShot+=MapEventHelper.OnConversationEnd;
                }).
                BeginPlayerOptions().
                PlayerOption(new TextObject("{=*}It is in a progress.",null),null).
                NpcLine(new TextObject("{=*}That's very good to hear![if:convo_merry]",null),null,null).
                CloseDialog().
                PlayerOption(new TextObject("{=*}Let's discuss the reparation.",null),null).
                NpcLine(new TextObject("{=*}I aggree.[if:convo_happy]",null),null,null).
                CloseDialog().GotoDialogState("peasant_revenge_discuss_reparation_start").
                PlayerOption(new TextObject("{=*}Let's discuss the fate of the raiders.",null),null).
                NpcLine(new TextObject("{=*}I aggree.[if:convo_happy]",null),null,null).
                CloseDialog().GotoDialogState("peasant_revenge_discuss_fate_start").
                PlayerOption(new TextObject("{=*}I need to check something actually.",null),null).
                NpcLine(new TextObject("{=*}Came back, when you have something.",null),null,null).
                CloseDialog().
                EndPlayerOptions().
                CloseDialog();
            }

            private DialogFlow GetNotableDiscussReparationDialogFlow()
            {
                DialogFlow dialog = DialogFlow.CreateDialogFlow("peasant_revenge_discuss_reparation_start",125);

                dialog.AddDialogLine(
                    "peasant_revenge_discuss_reparation_start_id",
                    "peasant_revenge_discuss_reparation_start",
                    "peasant_revenge_discuss_reparation_pl_options",
                    "{=*}What do we need to discuss here?[if:convo_shocked]",null,
                    null,
                    this,100,null,null,null);

                dialog.AddPlayerLine(
                    "peasant_revenge_discuss_reparation_pl_options_0",
                    "peasant_revenge_discuss_reparation_pl_options",
                    "close_window",
                    "{=*}I changed my mind.",null,null,this,100,null,null,null);
                return dialog;
            }

            private DialogFlow GetNotableDiscussFateOfRaidersDialogFlow()
            {
                DialogFlow dialog = DialogFlow.CreateDialogFlow("peasant_revenge_discuss_fate_start",125);

                dialog.AddDialogLine(
                    "peasant_revenge_discuss_fate_start_id",
                    "peasant_revenge_discuss_fate_start",
                    "peasant_revenge_discuss_fate_pl_options",
                    "{=*}What do we need to discuss here?[if:convo_shocked]",null,
                    null,
                    this,100,null,null,null);
                dialog.AddPlayerLine(
                    "peasant_revenge_discuss_fate_pl_options_0",
                    "peasant_revenge_discuss_fate_pl_options",
                    "close_window",
                    "{DISCUSS_LET_KILL_RAIDER}",
                    () =>
                    {
                        if(_targetRaider!=Hero.MainHero)
                        {
                            TextObject text = new TextObject("{=*}You can have the raiders head.");
                            MBTextManager.SetTextVariable("DISCUSS_LET_KILL_RAIDER",text);
                        }
                        else
                        {
                            TextObject text = new TextObject("{=*}You can have my head.");
                            MBTextManager.SetTextVariable("DISCUSS_LET_KILL_RAIDER",text);
                        }
                        return true;
                    },
                    () => { 
                        ExecuteHero(base.QuestGiver,this._targetRaider); 
                        CompleteQuestConsequences(); },this,100,null,null,null);
                dialog.AddPlayerLine(
                   "peasant_revenge_discuss_fate_pl_options_1",
                   "peasant_revenge_discuss_fate_pl_options",
                   "close_window",
                   "{=*}I changed my mind.",null,null,this,100,null,null,null);
                return dialog;
            }

            private void ExecuteHero(Hero executioner,Hero victim)
            {
                if(victim != Hero.MainHero)
                {
                    MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(executioner,victim,SceneNotificationData.RelevantContextType.Map));
                    KillCharacterAction.ApplyByExecution(victim,executioner,true,true);
                }
                else
                {
                    KillCharacterAction.ApplyByExecution(victim,executioner,true,false);
                }
               
            }

            private void CompleteQuestConsequences()
            {                
                base.CompleteQuestWithSuccess();
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
