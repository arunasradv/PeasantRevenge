using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
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
    public class NotableWantRevengeIssueQuest : QuestBase
    {
        public enum event_status
        {
            none,
            bribe,
            teach_to_revenge,
            teach_to_not_revenge,
            show_example_success,
            show_example_fail,
            bribe_success,
            bribe_fail,
            accusation,
            accusation_fail,
            accusation_success
        }

        [SaveableField(10)]
        private Settlement _targetSettlement; // The raided village

        [SaveableField(11)]
        private Hero _targetHero; // Hero who raided the village

        [SaveableField(13)]
        private JournalLog _startQuestLog;

        [SaveableField(14)]
        private CampaignTime _questGiverTravelStart;

        [SaveableField(15)]
        private Hero _accusedHeroByTargetHero; // Hero who got accused of the crime by _targetHero           

        private event_status pr_event_status = event_status.none;

        public NotableWantRevengeIssueQuest (string questId ,Hero questGiver ,CampaignTime duration ,int rewardGold ,
            Settlement targetSettlement ,Hero targetRaider) : base (questId ,questGiver ,duration ,rewardGold)
        {
            this._targetSettlement = targetSettlement;
            this._targetHero = targetRaider;

            base.AddTrackedObject (base.QuestGiver);
            this.SetDialogs ( );
            base.InitializeQuestOnCreation ( );
        }

        public override TextObject Title
        {
            get
            {
                TextObject textObject = new TextObject("{=*}{QUEST_GIVER.NAME}'s revenge",null);
                Helpers.StringHelpers.SetCharacterProperties ("QUEST_GIVER" ,base.QuestGiver.CharacterObject ,textObject ,false);
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
        protected override void HourlyTick ()
        {
            RevengerTravelProgress ( );
        }

        protected override void InitializeQuestOnGameLoad ()
        {
            this.SetDialogs ( );
        }

        protected override void RegisterEvents ()
        {
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener (this ,HeroPrisonerTaken);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener (this ,HeroPrisonerReleased);
            CampaignEvents.VillageBeingRaided.AddNonSerializedListener (this ,VillageBeingRaided);
            CampaignEvents.ConversationEnded.AddNonSerializedListener (this ,ConversationEnded);
            //TODO: Add check for player, if player is waitting in settlement, revenger should try to ambush the player... 
        }

        private void ConversationEnded (IEnumerable<CharacterObject> obj)
        {
            if(base.JournalEntries != null)
            {
                if(base.JournalEntries.Last ( ).LogText.Equals (IssueCancelText))
                {
                    CompleteQuestWithCancelConsequences ( );
                }
                else if(base.JournalEntries.Last ( ).LogText.Equals (IssueSuccessText))
                {
                    CompleteQuestWithSuccessConsequences ( );
                }
                else if(base.JournalEntries.Last ( ).LogText.Equals (IssueFailText))
                {
                    CompleteQuestWithFailConsequences ( );
                }
                else if(base.JournalEntries.Last ( ).LogText.Equals (IssueBetrayText))
                {
                    CompleteQuestWithBetrayConsequences ( );
                }
            }
        }

        private void VillageBeingRaided (Village village)
        {
            //if(this._targetSettlement.Village.Bound.BoundVillages.Any((x)=>(x.StringId.Equals(village.StringId))))
            //    return;

            if(village.Settlement.LastAttackerParty == null || village.Settlement.LastAttackerParty.Party.LeaderHero == null)
                return;

            if(this._targetHero == null) //TODO: add list of raiders , so quest could be updated with new raiders.
            {
                this._targetHero = village.Settlement.LastAttackerParty.Party.LeaderHero;
                AddHeroToQuestLog (this._targetHero);
            }
        }

        private void HeroPrisonerReleased (Hero prisoner ,PartyBase party ,IFaction faction ,EndCaptivityDetail detail)
        {
            UpdateJournalProgress (prisoner ,0);
            RevengerStopTraveling ( );
        }

        private void HeroPrisonerTaken (PartyBase party ,Hero prisoner)
        {
            if(UpdateJournalProgress (prisoner ,1))
            {
                RevengerTravelStartCounter ( );
            }
        }

        protected TextObject IssueOwnerTravelStartLogText
        {
            get
            {
                return new TextObject ("{=*}Revenger will learn about raiders capture soon." ,null);
            }
        }
        protected TextObject IssueOwnerTravelingLogText
        {
            get
            {
                return new TextObject ("{=*}Revenger is traveling." ,null);
            }
        }

        protected TextObject IssueOwnerTravelCriminalKilledLogText
        {
            get
            {
                return new TextObject ("{=*}The criminal has been killed." ,null);
            }
        }

        protected TextObject IssueOwnerTravelCriminalDodgedTheRevengerLogText
        {
            get
            {
                return new TextObject ("{=*}The criminal has been saved." ,null);
            }
        }

        protected TextObject IssueCancelText
        {
            get { return new TextObject ("{=*}You canceled the quest."); }
        }

        protected TextObject IssueSuccessText
        {
            get { return new TextObject ("{=*}You successfully completed the quest."); }
        }

        protected TextObject IssueFailText
        {
            get { return new TextObject ("{=*}You failed the quest."); }
        }

        protected TextObject IssueBetrayText
        {
            get { return new TextObject ("{=*}You betrayed someone and completed the quest."); }
        }

        public static bool HeroIsPlayersPrisoner (Hero hero)
        {
            if(hero == null)
                return false;
            return (Hero.MainHero.PartyBelongedTo?.Party?.PrisonerHeroes?.Contains (hero.CharacterObject) ?? false);
        }

        private int _get_reparation_value ()
        {
            int ReparationsScaleToSettlementHearts = 5;

            return (int)(base.QuestGiver.HomeSettlement.Village.Hearth * ReparationsScaleToSettlementHearts);
        }

        private void RevengerTravelStartCounter ()
        {
            _questGiverTravelStart = CampaignTime.HoursFromNow (10f); // TODO: Make travel start day dependant on how far raider has been captured from settlement.                
            base.AddLog (IssueOwnerTravelStartLogText);
        }

        private void RevengerStopTraveling ()
        {
            if(base.QuestGiver.PartyBelongedTo != null)
            {
                //base.AddLog(IssueOwnerTravelEndsLogText);
                base.QuestGiver.PartyBelongedTo.Ai.SetMoveGoToSettlement (_targetSettlement);
            }
        }

        private void RevengerTravelProgress ()
        {
            if(_questGiverTravelStart.IsPast)
            {
                if(base.QuestGiver.PartyBelongedTo == null && this._targetHero != null && this._targetHero.IsPrisoner)
                {


                    if(this._targetHero != null && this._targetHero.PartyBelongedToAsPrisoner != null && this._targetHero.PartyBelongedToAsPrisoner.MobileParty != null)
                    {
                        CreateNotableParty ( ); //TODO: party will be created even if criminal is captured with other AI party
                        base.QuestGiver.PartyBelongedTo.Ai.SetMoveEscortParty (this._targetHero.PartyBelongedToAsPrisoner.MobileParty);
                        base.AddLog (IssueOwnerTravelingLogText);
                    }
                }
                else if(base.QuestGiver.PartyBelongedTo != null)
                {
                    if(this._targetHero != null && this._targetHero.PartyBelongedToAsPrisoner != null && this._targetHero.PartyBelongedToAsPrisoner.MobileParty != null)
                    {
                        RevengerPartyMoveNearTarget (this._targetHero.PartyBelongedToAsPrisoner.MobileParty);

                        if(HeroIsPlayersPrisoner (this._targetHero))
                        {
                            //_start_conversation(this._targetRaider,base.QuestGiver); // wait until player start conversation
                        }
                        else if(this._targetHero == Hero.MainHero)
                        {
                            if(base.JournalEntries.Last ( ).LogText.Equals (IssueOwnerTravelingLogText))
                            { // Remember to Add new task after dialogue is completed.
                                _start_conversation (Hero.MainHero ,this._targetHero.PartyBelongedToAsPrisoner.LeaderHero);
                            }
                        }
                        else // criminal is other AI prisoner.
                        { //TODO: add dialog when player tries to talk to the prisoner, when prisoner is still captured by AI - create "Betray revenger branch"

                        }
                    }
                    else
                    {
                        //TODO: case where targetHero left prisoner state in mobile party (usualy transfered to the settlement...)
                    }
                }
            }
        }

        private void _start_conversation (Hero prisoner ,Hero other_hero)
        {
            if(prisoner.PartyBelongedToAsPrisoner == null || base.QuestGiver.PartyBelongedTo == null ||
                prisoner.PartyBelongedToAsPrisoner.MobileParty == null) // case, when prisoner is in settlement
                return;

            float peasantRevengePartyTalkToLordDistance = 2f;
            if(prisoner.PartyBelongedToAsPrisoner.MobileParty.Position2D.Distance (base.QuestGiver.PartyBelongedTo.Position2D) < peasantRevengePartyTalkToLordDistance)
            {
                CampaignMapConversation.OpenConversation (
                                    new ConversationCharacterData (prisoner.CharacterObject ,null ,false ,false ,false ,false ,false ,false) ,
                                    new ConversationCharacterData (other_hero.CharacterObject ,other_hero.PartyBelongedTo.Party ,false ,false ,false ,false ,false ,false));

            }
        }

        private void RevengerPartyMoveNearTarget (MobileParty mobileParty)
        {
            if(mobileParty != null && base.QuestGiver.PartyBelongedTo != null)
            {
                float peasantRevengePartyWaitLordDistance = 1f;
                Vec2 pposition = mobileParty.Position2D;
                Vec2 rvec2 = new Vec2(peasantRevengePartyWaitLordDistance>=0.0f ? peasantRevengePartyWaitLordDistance : 1.0f,0f);
                rvec2.RotateCCW (MBRandom.RandomFloatRanged (6.28f));
                base.QuestGiver.PartyBelongedTo.Ai.SetMoveGoToPoint (pposition + rvec2);
            }
        }

        private MobileParty CreateNotableParty ()
        {
            string revengerPartyNameStart = "Revenger_";
            int peasantRevengeMaxPartySize = 20;
            float _ignoreForHours = 6f;
            int size = (int)base.QuestGiver.HomeSettlement.Village.Hearth>=peasantRevengeMaxPartySize-1 ? peasantRevengeMaxPartySize-1 : (int)base.QuestGiver.HomeSettlement.Village.Hearth;
            MobileParty mobileParty = MobileParty.CreateParty($"{revengerPartyNameStart}{base.QuestGiver.Name}".Replace(' ','_'),null,null);
            CharacterObject villager = base.QuestGiver.CharacterObject.Culture.Villager;
            TroopRoster troopRoster = new TroopRoster(mobileParty.Party);
            TextObject textObject = new TextObject("{=PRev0085}Revenger",null);
            troopRoster.AddToCounts (base.QuestGiver.CharacterObject ,1 ,true ,0 ,0 ,true ,-1);
            troopRoster.AddToCounts (villager ,size ,false ,0 ,0 ,true ,-1);
            mobileParty.InitializeMobilePartyAtPosition (troopRoster ,new TroopRoster (mobileParty.Party) ,base.QuestGiver.HomeSettlement.Position2D);
            mobileParty.InitializePartyTrade (200);
            mobileParty.SetCustomName (textObject);
            mobileParty.SetCustomHomeSettlement (base.QuestGiver.HomeSettlement);
            mobileParty.SetPartyUsedByQuest (true);
            mobileParty.ShouldJoinPlayerBattles = false;
            mobileParty.ItemRoster.AddToCounts (MBObjectManager.Instance.GetObject<ItemObject> ("sumpter_horse") ,size);
            mobileParty.ItemRoster.AddToCounts (MBObjectManager.Instance.GetObject<ItemObject> ("butter") ,size);
            mobileParty.ItemRoster.AddToCounts (MBObjectManager.Instance.GetObject<ItemObject> ("cheese") ,size);
            mobileParty.IgnoreForHours (_ignoreForHours);
            mobileParty.Ai.SetDoNotMakeNewDecisions (true);
            mobileParty.Party.SetVisualAsDirty ( );
            mobileParty.Party.SetCustomOwner (base.QuestGiver);
            mobileParty.Aggressiveness = 0f;
            return mobileParty;
        }

        //TODO: Finish setting dialogs
        //TODO: Fix revenger party, when quest is completed and it i can be attacked (result crash if not fixed).
        private bool NotableDialogCondition ()
        {
            return Hero.OneToOneConversationHero == base.QuestGiver;
        }

        private bool CapturerPartyLeaderDialogCondition ()
        {


            bool can_party_leader_start_dialogue = (Hero.OneToOneConversationHero!=null&&
                    (this._targetHero!=null&&this._targetHero.PartyBelongedToAsPrisoner!=null&&Hero.OneToOneConversationHero==this._targetHero.PartyBelongedToAsPrisoner.LeaderHero));

            return can_party_leader_start_dialogue;
        }

        private void QuestAcceptedConsequences ()
        {
            base.StartQuest ( );

            TextObject textObject;
            //TODO: add variations what are dependant on village damage level, notable traits, relations with player, kingdom...
            if(this._targetHero != null)
            {
                if(HeroIsPlayersPrisoner (this._targetHero)) /*PLAYER HAS RAIDER AS PRISONER*/
                {
                    textObject = new TextObject ("{=*}You have captured {TARGET_HERO.NAME}, who has been raiding {TARGET_SETTLEMENT} village." ,null);
                }
                else
                {
                    if(this._targetHero.CharacterObject.IsPlayerCharacter) /*PLAYER IS THE RAIDER*/
                    {
                        textObject = new TextObject ("{=*}{QUEST_GIVER.LINK} said {?QUEST_GIVER.GENDER}she{?}he{\\?} will get revenge on you someday!" ,null);
                    }
                    else  /*PLAYER DOES NOT HAVE THE RAIDER AS PRISONER*/
                    {
                        textObject = new TextObject ("{=*}{QUEST_GIVER.LINK} asked you to capture {TARGET_HERO.NAME}, who has been raiding the {TARGET_SETTLEMENT} village recently." ,null);
                    }
                }
            }
            else /*RAIDER IS NOT KNOWN*/
            {
                textObject = new TextObject ("{=*}{QUEST_GIVER.LINK} want to give justice to the raiders, who are raiding the {TARGET_SETTLEMENT} village." ,null);
            }

            textObject.SetTextVariable ("TARGET_SETTLEMENT" ,this._targetSettlement.Name);
            if(this._targetHero != null)
            {
                StringHelpers.SetCharacterProperties ("TARGET_HERO" ,this._targetHero.CharacterObject ,textObject ,false);
            }
            StringHelpers.SetCharacterProperties ("QUEST_GIVER" ,base.QuestGiver.CharacterObject ,textObject ,false);
            StringHelpers.SetCharacterProperties ("PLAYER" ,Hero.MainHero.CharacterObject ,textObject ,false);

            this._startQuestLog = base.AddLog (textObject);

            AddHeroToQuestLog (this._targetHero);


            if(HeroIsPlayersPrisoner (this._targetHero))
                UpdateJournalProgress (this._targetHero ,1);

            //this._checkForMissionEnd=true; //TODO: check if I need to use it.
        }

        private void AddHeroToQuestLog (Hero hero)
        {
            if(hero == null)
                return;

            string text = "{=*}{TARGET_HERO.LINK} of {CLAN}";
            TextObject textObject = new TextObject(text,null);
            StringHelpers.SetCharacterProperties ("TARGET_HERO" ,hero.CharacterObject ,textObject ,false);
            textObject.SetTextVariable ("CLAN" ,hero.Clan?.EncyclopediaLinkWithName);
            string taskText = "{=*}{NAME}";
            TextObject taskTextObject = new TextObject(taskText,null);
            StringHelpers.SetCharacterProperties ("NAME" ,hero.CharacterObject ,taskTextObject ,false);

            if(base.JournalEntries.Where ((x) => (x.TaskName?.ToString ( ).Equals (taskTextObject.ToString ( )) ?? false)).Count ( ) == 0)
            {
                this._startQuestLog = base.AddDiscreteLog (textObject ,taskTextObject ,0 ,1 ,null ,false);
            }
        }

        /// <summary>
        /// Updates hero task status in quest.
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="status"></param>
        /// <returns>true, if status was updated or created</returns>
        private bool UpdateJournalProgress (Hero hero ,int status)
        {
            if(hero == this._targetHero)
            {
                UpdateJournalDirectionsProgress (null ,hero.Name.ToString ( ) ,status);
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
        private void UpdateJournalDirectionsProgress (string text ,string taskName ,int status)
        {
            if(base.JournalEntries.Where ((x) => (x.TaskName?.ToString ( ).Equals (taskName) ?? false)).Count ( ) == 0 && text != null)
            {
                this._startQuestLog = base.AddDiscreteLog (
                    new TextObject (text) ,
                    new TextObject (taskName) ,status ,1 ,null ,false);
            }
            else
            {
                UpdateTaskProgress (taskName ,status); // if in case quest already has this direction task, just update 
            }
        }

        private void UpdateTaskProgress (string taskName ,int status)
        {
            int journalLogIndex = base.JournalEntries.FindIndex((x) => (x.TaskName!=null ? x.TaskName.ToString()==taskName.ToString() : false));
            if(journalLogIndex >= 0 && journalLogIndex < base.JournalEntries.Count)
            {
                base.JournalEntries [journalLogIndex].UpdateCurrentProgress (status);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine ($"Task name: {taskName} was not found and not updated to status {status}.");
            }
        }

        // Conversation with Raider

        protected override void SetDialogs ()
        {

            Campaign.Current.ConversationManager.AddDialogFlow (GetPlayerDecideTheFateOfRaidersDialogFlow ( ) ,this);
            Campaign.Current.ConversationManager.AddDialogFlow (GetPlayerDiscussRevengerDemandsDialogFlow ( ) ,this);
            Campaign.Current.ConversationManager.AddDialogFlow (GetPlayerAccuseHeroPersuasionDialogFlow ( ) ,this);
            Campaign.Current.ConversationManager.AddDialogFlow (GetPlayerGetToCapturedRaiderDialogFlow ( ) ,this);

            //TODO: Fix dialogs, when revenger party is traveling to other AI capturer Party (and player init the conversation)

            this.OfferDialogFlow = DialogFlow.CreateDialogFlow ("issue_classic_quest_start" ,100).
                NpcLine (new TextObject ("{=*}I'm delighted." ,null) ,null ,null). //TODO: Change text for player is raider case!
                Condition (new ConversationSentence.OnConditionDelegate (this.NotableDialogCondition)).
                Consequence (new ConversationSentence.OnConsequenceDelegate (this.QuestAcceptedConsequences)).CloseDialog ( );

            this.DiscussDialogFlow = DialogFlow.CreateDialogFlow ("quest_discuss" ,100).
                NpcLine (new TextObject ("{=*}How are you dealing with my problem?[if:convo_delighted][ib:hip]" ,null) ,null ,null).
                Condition (new ConversationSentence.OnConditionDelegate (this.NotableDialogCondition)).
                Consequence (delegate
                {
                    Campaign.Current.ConversationManager.ConversationEndOneShot += MapEventHelper.OnConversationEnd;
                }).
            BeginPlayerOptions ( ).
            PlayerOption (new TextObject ("{=*}It is in a progress." ,null) ,null).
            NpcLine (new TextObject ("{=*}That's very good to hear![if:convo_merry]" ,null) ,null ,null).
            CloseDialog ( ).
            PlayerOption (new TextObject ("{=*}Let's discuss it." ,null) ,null).
            NpcLine (new TextObject ("{=*}I aggree.[if:convo_happy]" ,null) ,null ,null).
            CloseDialog ( ).GotoDialogState ("peasant_revenge_discuss_fate_start").
            PlayerOption (new TextObject ("{=*}I need to check something actually." ,null) ,null).
            NpcLine (new TextObject ("{=*}Came back, when you have something." ,null) ,null ,null).
            CloseDialog ( ).
            EndPlayerOptions ( ).
            CloseDialog ( );
        }

        /// <summary>
        /// discussing the quest with targetHero while hero is not prisoner of player
        /// If hero is not prisoner:
        /// * 
        /// If hero is prisoner of other AI hero party:
        /// *
        /// </summary>
        /// <returns></returns>
        private DialogFlow GetPlayerGetToCapturedRaiderDialogFlow ()
        {
            DialogFlow dialog = DialogFlow.CreateDialogFlow("hero_main_options",100);

            //NpcLine(new TextObject("{=*}What? Who the hell are you?[rf:idle_angry][ib:closed][if:idle_angry]",null),null,null).
            // Condition(new ConversationSentence.OnConditionDelegate(this.CapturerPartyLeaderDialogCondition)).GotoDialogState("peasant_revenge_discuss_pr_demands_pl_options");
           
            //dialog.AddDialogLine (
            //   "peasant_revenge_talk_to_captured_start_id" ,
            //   "peasant_revenge_talk_to_captured_start" ,
            //   "peasant_revenge_talk_to_captured_pl_options" ,
            //   "{=*}What do we need to discuss here?[if:convo_shocked]" ,
            //   null ,
            //   null ,
            //   this ,100 ,null ,null ,null);          

            dialog.AddPlayerLine (
                "peasant_revenge_talk_to_captured_start" ,
                "hero_main_options" ,
                "peasant_revenge_talk_to_captured_hero_reaction_0" ,
                "{TALK_TO_CAPTURED_RAIDER_START}" ,
                () =>
                {
                    if(this._targetHero.CharacterObject.IsPlayerCharacter == false &&
                    HeroIsPlayersPrisoner (this._targetHero) == false &&
                    Hero.OneToOneConversationHero == this._targetHero)
                    { 
                        TextObject text = new TextObject("{=*}It is time for {QUEST_GIVER.NAME}'s revenge!");/*For identifying the unknown cases*/

                        text.SetCharacterProperties ("QUEST_GIVER" ,QuestGiver.CharacterObject);

                        if(this._targetHero.PartyBelongedTo != null && this._targetHero.PartyBelongedToAsPrisoner == null) /*hero is with party and not prisoner*/
                        {
                            text = new TextObject ("{=*}I have complaints from a village you have raided.");
                        }
                        else if(this._targetHero.PartyBelongedToAsPrisoner != null) /*Target hero is prisoner*/
                        {
                            text = new TextObject ("{=*}You are in big trouble."); 
                        }
                        
                        MBTextManager.SetTextVariable ("TALK_TO_CAPTURED_RAIDER_START" ,text);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                } ,null,this ,120 ,null ,null ,null);
           
            dialog.AddDialogLine (
              "peasant_revenge_talk_to_captured_hero_reaction_0_id" ,
              "peasant_revenge_talk_to_captured_hero_reaction_0" ,
              "peasant_revenge_talk_to_captured_hero_reaction_pl_options" ,
              "{=*}What do we need to discuss here?[if:convo_shocked]" ,null ,
              null ,
              this ,100 ,null ,null ,null);

            /*TODO: Talk with target hero how hero could pay reparation...*/


            return dialog;
        }

            /// <summary>
            /// discussing the quest progress with quest giver
            /// - Let kill the raider (player or AI)
            /// - Pay (raider (player or AI))
            /// - Pay (in place of raider (AI)) // TODO: Need persuation
            /// * Blame other AI (player or AI) // TODO: Need persuation
            /// * Persuade the questGiver to drop the revenge  // TODO: Need persuation
            /// - Abandon the quest
            /// - Return to previous meniu
            /// </summary>
            /// TODO: fix crash when encountered quest pary whitch is now enemy and map event is deciding if party should join battle.
            /// <returns></returns>
            private DialogFlow GetPlayerDecideTheFateOfRaidersDialogFlow ()
        {
            DialogFlow dialog = DialogFlow.CreateDialogFlow("peasant_revenge_discuss_fate_start",125);

            dialog.AddDialogLine (
                "peasant_revenge_discuss_fate_start_id" ,
                "peasant_revenge_discuss_fate_start" ,
                "peasant_revenge_discuss_fate_pl_options" ,
                "{=*}What do we need to discuss here?[if:convo_shocked]" ,null ,
                null ,
                this ,100 ,null ,null ,null);
            /*RAIDER DIE*/
            dialog.AddPlayerLine (
                "peasant_revenge_discuss_fate_pl_options_raider_kill" ,
                "peasant_revenge_discuss_fate_pl_options" ,
                "close_window" ,
                "{DISCUSS_LET_KILL_RAIDER}" ,
                () =>
                {
                    if(this._targetHero == Hero.MainHero)
                    {
                        TextObject text = new TextObject("{=*}You can have my head.");
                        MBTextManager.SetTextVariable ("DISCUSS_LET_KILL_RAIDER" ,text);
                        return true;
                    }
                    else
                    {
                        TextObject text = new TextObject("{=*}{TARGET_HERO.NAME} should die.");
                        StringHelpers.SetCharacterProperties ("TARGET_HERO" ,this._targetHero.CharacterObject ,text ,false);
                        MBTextManager.SetTextVariable ("DISCUSS_LET_KILL_RAIDER" ,text);
                        return HeroIsPlayersPrisoner (this._targetHero);
                    }
                } ,
                () =>
                {
                    ExecuteHero (base.QuestGiver ,this._targetHero);
                    CompleteQuestWithSuccessConsequences ( );
                } ,this ,100 ,null ,null ,null);
            /*RAIDER PAY*/
            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_options_raider_pay" ,
               "peasant_revenge_discuss_fate_pl_options" ,
               "peasant_revenge_discuss_fate_pl_options_raider_pay_peasant_received_pay" ,
               "{DISCUSS_LET_PAY_RAIDER}" ,
               () =>
               {
                   if(_targetHero == Hero.MainHero)
                   {
                       TextObject text = new TextObject("{=*}I'll pay {REPARATION}{GOLD_ICON}.");
                       MBTextManager.SetTextVariable ("REPARATION" ,_get_reparation_value ( ) ,text);
                       MBTextManager.SetTextVariable ("DISCUSS_LET_PAY_RAIDER" ,text);
                       return true;
                   }
                   else
                   {
                       TextObject text = new TextObject("{=*}{TARGET_HERO.NAME} will pay {REPARATION}{GOLD_ICON}.");
                       StringHelpers.SetCharacterProperties ("TARGET_HERO" ,this._targetHero.CharacterObject ,text ,false);
                       MBTextManager.SetTextVariable ("REPARATION" ,_get_reparation_value ( ) ,text);
                       MBTextManager.SetTextVariable ("DISCUSS_LET_PAY_RAIDER" ,text);
                       return HeroIsPlayersPrisoner (this._targetHero);
                   }

               } ,null ,this ,100 ,null ,null ,null);
            /*PAY IN PLACE OF AI*/
            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_options_in_raiders_place_pay" ,
               "peasant_revenge_discuss_fate_pl_options" ,
               "peasant_revenge_discuss_fate_pl_options_raider_pay_peasant_receiving_pay" ,
               "{DISCUSS_PAY_IN_RAIDER}" ,
               () =>
               {
                   if(_targetHero != Hero.MainHero /*&& !HeroIsPlayersPrisoner(this._targetHero)*/)
                   {
                       TextObject text = new TextObject("{=*}I'll pay {REPARATION}{GOLD_ICON} in place of {TARGET_HERO.NAME}.");
                       StringHelpers.SetCharacterProperties ("TARGET_HERO" ,this._targetHero.CharacterObject ,text ,false);
                       MBTextManager.SetTextVariable ("REPARATION" ,_get_reparation_value ( ) ,text);
                       MBTextManager.SetTextVariable ("DISCUSS_PAY_IN_RAIDER" ,text);

                       return true;
                   }
                   else
                   {
                       return false;
                   }

               } ,null ,this ,100 ,null ,null ,null);
            /*BLAME*/
            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_options_blame" ,
               "peasant_revenge_discuss_fate_pl_options" ,
               "peasant_revenge_discuss_fate_pl_blame" ,
               "{=*}But {TARGET_HERO.NAME} may be not the criminal..." ,
               () =>
               {
                   StringHelpers.SetCharacterProperties ("TARGET_HERO" ,this._targetHero.CharacterObject);
                   return true;
               } ,
               null ,this ,100 ,(out TextObject hintText) =>
               {
                   hintText = new TextObject ("{=*} Blame other hero for the crime.");
                   return true;
               } ,null ,null);

            dialog.AddDialogLine (
               "peasant_revenge_discuss_fate_pl_blame_id" ,
               "peasant_revenge_discuss_fate_pl_blame" ,
               "peasant_revenge_discuss_fate_pl_blame_options" ,
               "{=*}Who is then?[if:convo_furious]" ,null ,
               null ,
               this ,100 ,null ,null ,null);

            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_blame_options_0" ,
               "peasant_revenge_discuss_fate_pl_blame_options" ,
               "close_window" ,
               "{=*}{accuseD0.NAME}" ,
               () => { return _hero_can_accuse_condition (this._targetHero ,0); } ,
               () =>
               {
                   Hero hero_accused = get_prisoner_blamed(this._targetHero,0);
                   TextObject text = new TextObject("{=*}You blamed {accuseD0.LINK} for the crime.");
                   StringHelpers.SetCharacterProperties ($"accuseD{0}" ,hero_accused.CharacterObject ,text);
                   base.AddLog (text);
                   ExecuteHero (this.QuestGiver ,hero_accused);
                   base.AddLog (IssueSuccessText);
               } ,
               this ,100 ,null ,null);

            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_blame_options_1" ,
               "peasant_revenge_discuss_fate_pl_blame_options" ,
               "close_window" ,
               "{=*}{accuseD1.NAME}" ,
               () => { return _hero_can_accuse_condition (this._targetHero ,1); } ,
               () =>
               {
                   Hero hero_accused = get_prisoner_blamed(this._targetHero,1);
                   TextObject text = new TextObject("{=*}You blamed {accuseD1.LINK} for the crime.");
                   StringHelpers.SetCharacterProperties ($"accuseD{1}" ,hero_accused.CharacterObject ,text);
                   base.AddLog (text);
                   ExecuteHero (this.QuestGiver ,hero_accused);
                   base.AddLog (IssueSuccessText);
               } ,
               this ,100 ,null ,null);

            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_blame_options_2" ,
               "peasant_revenge_discuss_fate_pl_blame_options" ,
               "close_window" ,
               "{=*}{accuseD2.NAME}" ,
               () => { return _hero_can_accuse_condition (this._targetHero ,2); } ,
               () =>
               {
                   Hero hero_accused = get_prisoner_blamed(this._targetHero,2);
                   TextObject text = new TextObject("{=*}You blamed {accuseD2.LINK} for the crime.");
                   StringHelpers.SetCharacterProperties ($"accuseD{2}" ,hero_accused.CharacterObject ,text);
                   base.AddLog (text);
                   ExecuteHero (this.QuestGiver ,hero_accused);
                   base.AddLog (IssueSuccessText);
               } ,
               this ,100 ,null ,null);

            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_blame_options_3" ,
               "peasant_revenge_discuss_fate_pl_blame_options" ,
               "close_window" ,
               "{=*}{accuseD3.NAME}" ,
               () => { return _hero_can_accuse_condition (this._targetHero ,3); } ,
               () =>
               {
                   Hero hero_accused = get_prisoner_blamed(this._targetHero,3);
                   TextObject text = new TextObject("{=*}You blamed {accuseD3.LINK} for the crime.");
                   StringHelpers.SetCharacterProperties ($"accuseD{3}" ,hero_accused.CharacterObject ,text);
                   base.AddLog (text);
                   ExecuteHero (this.QuestGiver ,hero_accused);
                   base.AddLog (IssueSuccessText);
               } ,
               this ,100 ,null ,null);
            /*When method get_prisoner_blamed fails to get the prisoner , we can blame any other prisoner in the target hero party prisoner roster*/
            dialog.AddPlayerLine (
              "peasant_revenge_discuss_fate_pl_blame_options_4" ,
              "peasant_revenge_discuss_fate_pl_blame_options" ,
              "close_window" ,
              "{=*}{accuseD0.NAME}" ,
              () => {
                  return _hero_can_accuse_prisoner_condition (Hero.MainHero ,this._targetHero ,0) &&
                  !_hero_can_accuse_condition (this._targetHero ,0);
              } ,
              () =>
              {
                  Hero hero_accused = get_any_prisoner_to_be_blamed(Hero.MainHero,this._targetHero,0);
                  TextObject text = new TextObject("{=*}You blamed {accuseD0.LINK} for the crime.");
                  StringHelpers.SetCharacterProperties ($"accuseD{0}" ,hero_accused.CharacterObject ,text);
                  base.AddLog (text);
                  ExecuteHero (this.QuestGiver ,hero_accused);
                  base.AddLog (IssueSuccessText);
              } ,
              this ,100 ,null ,null);

            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_blame_options_n" ,
               "peasant_revenge_discuss_fate_pl_blame_options" ,
               "quest_discuss" ,
               "{=*}I will find out soon." ,null ,null ,this ,100 ,null ,null ,null);
            /*DROP REVENGE*/
            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_options_drop" ,
               "peasant_revenge_discuss_fate_pl_options" ,
               "peasant_revenge_discuss_fate_stop_or_else" ,
               "{=*}You should drop your revenge, or else..." ,null ,null ,this ,100 ,null ,null ,null);

            dialog.AddDialogLine (
             "peasant_revenge_any_revenger_or_else" ,
             "peasant_revenge_discuss_fate_stop_or_else" ,
             "peasant_revenge_discuss_fate_stop_or_else_options" ,
             "{=*}What else?[rf:idle_angry][ib:closed][if:idle_angry]" ,null ,null ,this ,200 ,null ,null ,null);

            dialog.AddPlayerLine (
             "peasant_revenge_discuss_fate_stop_or_else_options_0" ,
             "peasant_revenge_discuss_fate_stop_or_else_options" ,
             "close_window" ,
             "{=*}I will chop your head off!" ,
             null ,
             () =>
             {
                 TextObject text = new TextObject("{=*}You beheaded the {QUESTGIVER.LINK}.");
                 StringHelpers.SetCharacterProperties ($"QUESTGIVER" ,QuestGiver.CharacterObject ,text);
                 base.AddLog (text);
                 ExecuteHero (this._targetHero ,this.QuestGiver);
                 base.AddLog (IssueSuccessText);
             } ,
             this ,100 ,null ,null);

            dialog.AddPlayerLine (
              "peasant_revenge_discuss_fate_stop_or_else_options_1" ,
              "peasant_revenge_discuss_fate_stop_or_else_options" ,
               "close_window" ,
              "{=*}{EXECUTIONER.LINK} will chop your head off!" ,
              () => { return peasant_revenge_get_executioner_companion_condition ( ); } ,
              () => {
                  TextObject text = new TextObject("{=*}{EXECUTIONER.LINK} beheaded the {QUESTGIVER.LINK}.");
                  StringHelpers.SetCharacterProperties ($"QUESTGIVER" ,QuestGiver.CharacterObject ,text);
                  base.AddLog (text);
                  ExecuteHero (get_first_companion ( ) ,this.QuestGiver);
                  base.AddLog (IssueSuccessText);
              } ,this ,100 ,null ,null);

#if PERSUADE_THE_PEASANT_TO_DROP_REVENGE

                dialog.AddPlayerLine(
                  "peasant_revenge_discuss_fate_stop_or_else_options_2",
                  "peasant_revenge_discuss_fate_stop_or_else_options",
                  "peasant_revenge_discuss_fate_stop_or_else_options_2_start_persuasion",
                  "{PAYER_COMMENT_REVENGE_TEACH}",
                  new ConversationSentence.OnConditionDelegate(peasant_revenge_player_not_happy_with_peasant_teach_condition),
                  new ConversationSentence.OnConsequenceDelegate(peasant_revenge_player_not_happy_with_peasant_teach_consequence),
                  125,
                  new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_start_teach_clickable));
#endif
            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_stop_or_else_options_n" ,
               "peasant_revenge_discuss_fate_stop_or_else_options" ,
               "quest_discuss" ,
               "{=*}Nevermind." ,null ,null ,this ,100 ,null ,null ,null);
            /*PAYING INSTEAD OF CRIMINAL*/
            dialog.AddDialogLine (
             "peasant_revenge_discuss_fate_pl_options_raider_pay_peasant_receiving_pay_success" ,
             "peasant_revenge_discuss_fate_pl_options_raider_pay_peasant_receiving_pay" ,
             "close_window" ,
             "{=PRev0037}I'm pleased.[if:convo_happy]" ,
             () => { return hero_would_accept_reparation_from_others_instead_of_criminal (this.QuestGiver ,this._targetHero); } ,
             () =>
             {
                 _pay_reparation (this._targetHero ,base.QuestGiver);
                 base.AddLog (IssueSuccessText);
             } ,this ,100 ,null ,null ,null);

            dialog.AddDialogLine (
             "peasant_revenge_discuss_fate_pl_options_raider_pay_peasant_receiving_pay_fail" ,
             "peasant_revenge_discuss_fate_pl_options_raider_pay_peasant_receiving_pay" ,
             "peasant_revenge_discuss_fate_pl_options" ,
             "{=*}{PL_PAY_INSTEAD_OF_CR}" ,
             () => { return !hero_would_accept_reparation_from_others_instead_of_criminal (this.QuestGiver ,this._targetHero); } ,
             () =>
             {
                 /* _pay_reparation(this._targetHero,base.QuestGiver);
                  base.AddLog(IssueSuccessText);*/
             } ,this ,100 ,null ,null ,null);

            /*CANCEL QUEST*/
            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_options_abandon" ,
               "peasant_revenge_discuss_fate_pl_options" ,
               "close_window" ,
               "{=*}I do not care about your revenge." ,null ,() =>
               {
                   base.AddLog (IssueCancelText);
               } ,this ,100 ,(out TextObject hintText) =>
               {
                   hintText = new TextObject ("{=*} Cancel the quest.");
                   return true;
               } ,null ,null);
            /*GO BACK*/
            dialog.AddPlayerLine (
               "peasant_revenge_discuss_fate_pl_options_n" ,
               "peasant_revenge_discuss_fate_pl_options" ,
               "quest_discuss" ,
               "{=*}Nevermind." ,null ,null ,this ,100 ,null ,null ,null);
            /*SUCCESS*/
            dialog.AddDialogLine (
             "peasant_revenge_discuss_fate_pl_options_raider_pay_peasant_received_pay_success" ,
             "peasant_revenge_discuss_fate_pl_options_raider_pay_peasant_received_pay" ,
             "close_window" ,
             "{=PRev0037}I'm pleased.[if:convo_happy]" ,
             null ,
             () =>
             {
                 _pay_reparation (this._targetHero ,base.QuestGiver);
                 base.AddLog (IssueSuccessText);
             } ,this ,100 ,null ,null ,null);
            return dialog;

        }



        /// <summary>
        /// When player is captured by other AI, and hero start dialogue, because questGiver party arrive to execute the revenge.          
        /// - Pay
        /// - Not Pay (AI hero may let player live)
        /// - Other AI pays the reparation  // TODO: Need persuation
        /// * Blame other AI prisoner (persuade the AI hero?)                        TODO: persuade the lord "player is not the criminal"
        ///     * Mobile party hero who captured the criminal is checking if the acussation is valid one.The mobile party hero will:
        ///         * Agree with acussation without questions asked (has traits and relations, and situation of clans involved agree)
        ///         Persuation options otherwise:
        ///         * Target hero blame accused hero traits.
        ///         * Target hero blame accused hero relations with raided village clan.
        ///         * Target hero blame accused hero relations with raided notable.
        ///         * Target hero blame the clan/kingdom decision.
        ///         * Try sound convincing.
        /// * Cannot accept to kill because blamed or criminal due to conflict.      TODO: all  // TODO: Need persuation
        /// * Return to previous meniu (after not confirmed blame, not succesfull barter)
        /// </summary>
        /// <returns></returns>
        private DialogFlow GetPlayerDiscussRevengerDemandsDialogFlow ()
        {
            DialogFlow dialog = DialogFlow.CreateDialogFlow("start",125).
                    NpcLine(new TextObject("{=PRev0001}You looted nearby village. Peasants demand to cut someone's head off. What will you say?[rf:idle_angry][ib:closed][if:idle_angry]",null),null,null).
                    Condition(new ConversationSentence.OnConditionDelegate(this.CapturerPartyLeaderDialogCondition)).GotoDialogState("peasant_revenge_discuss_pr_demands_pl_options");

            /*PAY*/
            dialog.AddPlayerLine (
              "peasant_revenge_discuss_pr_demands_pl_options_pl_pay" ,
              "peasant_revenge_discuss_pr_demands_pl_options" ,
              "peasant_revenge_discuss_pr_demands_pl_pay_barter_line" ,
              "{=*}I'll pay {REPARATION}{GOLD_ICON}." ,new ConversationSentence.OnConditionDelegate (_hero_reparation_condition) ,null ,this ,100 ,null);

            dialog.AddDialogLine (
              "peasant_revenge_discuss_pr_demands_pl_pay_line" ,
              "peasant_revenge_discuss_pr_demands_pl_pay_barter_line" ,
              "peasant_revenge_discuss_pr_demands_pl_pay_received_pay" ,
              "{=!}BARTER LINE - Covered by barter interface. Please do not remove these lines!" ,
              null ,
              new ConversationSentence.OnConsequenceDelegate (this._player_barter_consequence) ,this ,100 ,null ,null ,null);

            /*NOT PAY*/
            dialog.AddPlayerLine (
              "peasant_revenge_discuss_pr_demands_pl_options_not_pay" ,
              "peasant_revenge_discuss_pr_demands_pl_options" ,
              "peasant_revenge_discuss_pr_demands_pl_not_pay" ,
              "{=*}I'll not pay." ,null ,null ,this ,100 ,
              (out TextObject hintText) => { return this._hero_will_not_pay_reparation_on_clickable (_get_victim ( ) ,Hero.OneToOneConversationHero ,out hintText); });

            /*OTHER AI PAY*/
            dialog.AddPlayerLine (
              "peasant_revenge_discuss_pr_demands_pl_options_friend_pay" ,
              "peasant_revenge_discuss_pr_demands_pl_options" ,
              "peasant_revenge_discuss_pr_demands_friend_pay_choose" ,
              "{=PRev0065}I have friends, who will pay the reparation." ,
              () => { return !GetHeroSuportersWhoCouldPayUnpaidRansom (this._targetHero ,_get_reparation_value ( )).IsEmpty ( ); } ,
              null ,this ,110 ,
              new ConversationSentence.OnClickableConditionDelegate (this.peasant_revenge_criminal_has_suporters_clickable_condition) ,null);

            dialog.AddDialogLine (
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_lord" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_choose" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_options" ,
             "{=*}Who will pay?[ib:convo_thinking]" ,
             null ,
            null ,this ,100 ,null ,null ,null);

            dialog.AddPlayerLine (
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_lord_0" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_options" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_received" ,
             "{=*}{SAVER0.NAME}" ,
             () => { return _hero_has_supporter_condition (this._targetHero ,_get_reparation_value ( ) ,0); } ,
             () => { _pay_reparation (GetHeroSupporter (this._targetHero ,_get_reparation_value ( ) ,0) ,this.QuestGiver); } ,this ,100 ,null ,null);

            dialog.AddPlayerLine (
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_lord_1" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_options" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_received" ,
             "{=*}{SAVER1.NAME}" ,
              () => { return _hero_has_supporter_condition (this._targetHero ,_get_reparation_value ( ) ,1); } ,
             () => { _pay_reparation (GetHeroSupporter (this._targetHero ,_get_reparation_value ( ) ,1) ,this.QuestGiver); } ,this ,100 ,null ,null);

            dialog.AddPlayerLine (
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_lord_2" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_options" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_received" ,
             "{=*}{SAVER2.NAME}" ,
             () => { return _hero_has_supporter_condition (this._targetHero ,_get_reparation_value ( ) ,2); } ,
             () => { _pay_reparation (GetHeroSupporter (this._targetHero ,_get_reparation_value ( ) ,2) ,this.QuestGiver); } ,this ,100 ,null ,null);

            dialog.AddPlayerLine (
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_lord_3" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_choose_options" ,
             "peasant_revenge_discuss_pr_demands_pl_options" ,
             "{=*}I'm thinking about something else." ,null ,null ,this ,90 ,null ,null);

            dialog.AddDialogLine (
             "peasant_revenge_discuss_pr_demands_friend_pay_received_success" ,
             "peasant_revenge_discuss_pr_demands_friend_pay_received" ,
             "close_window" ,
             "{=*}You have a generous friends.[if:convo_happy]" ,
             null ,
             () => { base.AddLog (IssueSuccessText); } ,this ,100 ,null ,null ,null);

            /*BLAME*/
            dialog.AddPlayerLine (
             "peasant_revenge_discuss_pr_demands_pl_options_pl_blame" ,
             "peasant_revenge_discuss_pr_demands_pl_options" ,
             "peasant_revenge_discuss_pr_demands_pl_blame_ask_options" ,
             "{=*}I'm not a criminal..." ,null ,null ,this ,100 ,null);

            dialog.AddDialogLine (
            "peasant_revenge_discuss_pr_demands_pl_blame_ask_options_start" ,
            "peasant_revenge_discuss_pr_demands_pl_blame_ask_options" ,
            "peasant_revenge_discuss_pr_demands_pl_blame_options" ,
            "{=*}Who is then?[if:convo_thinking]" ,
            null ,null ,this ,100 ,null ,null ,null);

            dialog.AddPlayerLine (
            "peasant_revenge_discuss_pr_demands_pl_blame_choose_lord_0" ,
            "peasant_revenge_discuss_pr_demands_pl_blame_options" ,
            "peasant_revenge_player_accuse_start_persuasion" ,
            "{=*}{accuseD0.NAME}" ,
            () => { return _hero_can_accuse_condition (this._targetHero ,0); } ,
            () => {
                //TODO: No ending here , add the ending after persuation.
                //TODO: Add this to other options.
                this._accusedHeroByTargetHero = get_prisoner_blamed (this._targetHero ,0);
                peasant_revenge_player_accuse_hero_consequence ( );
#if false
                    Hero hero_accused = get_prisoner_blamed(this._targetHero,0);
                    TextObject text = new TextObject("{=*}You blamed {accuseD0.LINK} for the crime.");
                    StringHelpers.SetCharacterProperties($"accuseD{0}",hero_accused.CharacterObject,text);
                    base.AddLog(text);
                    ExecuteHero(this.QuestGiver,hero_accused);
                    base.AddLog(IssueSuccessText);
#endif
            } ,
            this ,100 ,null ,null);

            dialog.AddPlayerLine (
            "peasant_revenge_discuss_pr_demands_pl_blame_choose_lord_1" ,
            "peasant_revenge_discuss_pr_demands_pl_blame_options" ,
            "peasant_revenge_player_accuse_start_persuasion" ,
            "{=*}{accuseD1.NAME}" ,
            () => { return _hero_can_accuse_condition (this._targetHero ,1); } ,
            () => {
                this._accusedHeroByTargetHero = get_prisoner_blamed (this._targetHero ,1);
                peasant_revenge_player_accuse_hero_consequence ( );
#if false
                    Hero hero_accused = get_prisoner_blamed(this._targetHero,1);
                    TextObject text = new TextObject("{=*}You blamed {accuseD1.LINK} for the crime.");
                    StringHelpers.SetCharacterProperties($"accuseD{1}",hero_accused.CharacterObject,text);
                    base.AddLog(text);
                    ExecuteHero(this.QuestGiver,hero_accused);
                    base.AddLog(IssueSuccessText);
#endif
            } ,
            this ,100 ,null ,null);

            dialog.AddPlayerLine (
            "peasant_revenge_discuss_pr_demands_pl_blame_choose_lord_2" ,
            "peasant_revenge_discuss_pr_demands_pl_blame_options" ,
            "peasant_revenge_player_accuse_start_persuasion" ,
            "{=*}{accuseD2.NAME}" ,
            () => { return _hero_can_accuse_condition (this._targetHero ,2); } ,
            () => {
                this._accusedHeroByTargetHero = get_prisoner_blamed (this._targetHero ,2);
                peasant_revenge_player_accuse_hero_consequence ( );
#if false
                    Hero hero_accused = get_prisoner_blamed(this._targetHero,2);
                    TextObject text = new TextObject("{=*}You blamed {accuseD2.LINK} for the crime.");
                    StringHelpers.SetCharacterProperties($"accuseD{2}",hero_accused.CharacterObject,text);
                    base.AddLog(text);
                    ExecuteHero(this.QuestGiver,hero_accused);
                    base.AddLog(IssueSuccessText);
#endif
            } ,
            this ,100 ,null ,null);
            /*When cannot find proper prisoner, lets blame anybody*/
            dialog.AddPlayerLine (
           "peasant_revenge_discuss_pr_demands_pl_blame_choose_lord_3" ,
           "peasant_revenge_discuss_pr_demands_pl_blame_options" ,
           "peasant_revenge_player_accuse_start_persuasion" ,
           "{=*}{accuseD0.NAME}" ,
           () => {
               return _hero_can_accuse_prisoner_condition (Hero.MainHero ,this._targetHero ,0) &&
               !_hero_can_accuse_condition (this._targetHero ,0);
           } ,
           () => {
               this._accusedHeroByTargetHero = get_any_prisoner_to_be_blamed (Hero.MainHero ,this._targetHero ,0);
               peasant_revenge_player_accuse_hero_consequence ( );
#if false
                   Hero hero_accused = get_any_prisoner_to_be_blamed(Hero.MainHero,this._targetHero,0);
                   TextObject text = new TextObject("{=*}You blamed {accuseD0.LINK} for the crime.");
                   StringHelpers.SetCharacterProperties($"accuseD{0}",hero_accused.CharacterObject,text);
                   base.AddLog(text);
                   ExecuteHero(this.QuestGiver,hero_accused);
                   base.AddLog(IssueSuccessText);
#endif
           } ,
           this ,100 ,null ,null);

            dialog.AddPlayerLine (
             "peasant_revenge_discuss_pr_demands_pl_options_pl_blame_n" ,
             "peasant_revenge_discuss_pr_demands_pl_blame_options" ,
             "peasant_revenge_discuss_pr_demands_pl_options" ,
             "{=*}I will find out soon." ,null ,null ,this ,100 ,null);





            #region END

            dialog.AddDialogLine (
           "peasant_revenge_player_accuse_persuasion_ended_0" ,
           "peasant_revenge_player_accuse_persuasion_ended" ,
           "close_window" ,
           "{=*}Glad it ended here.[if:convo_happy]" ,
          () => { return _hero_can_accuse_condition (this._targetHero ,0); } ,
            () => {
                Hero hero_accused = (pr_event_status==event_status.accusation_success) ? this._accusedHeroByTargetHero : this._targetHero;
                TextObject text = new TextObject("{=*}You blamed {accuseD0.LINK} for the crime.");
                StringHelpers.SetCharacterProperties ($"accuseD{0}" ,hero_accused.CharacterObject ,text);
                base.AddLog (text);
                ExecuteHero (this.QuestGiver ,hero_accused);

                if(pr_event_status == event_status.accusation_success)
                {
                    base.AddLog (IssueSuccessText);
                }
                else
                {
                    base.AddLog (IssueFailText);
                }
            } ,this ,100 ,null ,null ,null);

            dialog.AddDialogLine (
                "peasant_revenge_discuss_pr_demands_pl_not_pay_answ" ,
                "peasant_revenge_discuss_pr_demands_pl_not_pay" ,
                "close_window" ,
                "{PL_NOT_PAY_LEADER_ANSW}" ,
                () =>
                {
                    return Hero.MainHero.CanDie (KillCharacterAction.KillCharacterActionDetail.Executed)
                    && _hero_will_not_pay_reparation_on_condition (_get_victim ( ) ,Hero.OneToOneConversationHero);
                } ,
                () =>
                {
                    if(_will_party_leader_let_the_criminal_die (_get_victim ( ) ,Hero.OneToOneConversationHero))
                    {
                        ExecuteHero (base.QuestGiver ,_get_victim ( ));
                        base.AddLog (IssueOwnerTravelCriminalKilledLogText);
                        base.AddLog (IssueFailText);
                    }
                    else
                    {
                        base.AddLog (IssueOwnerTravelCriminalDodgedTheRevengerLogText);
                        base.AddLog (IssueSuccessText);
                    }
                } ,this ,100 ,null);


            dialog.AddDialogLine (
             "peasant_revenge_discuss_pr_demands_pl_pay_received_pay_success" ,
             "peasant_revenge_discuss_pr_demands_pl_pay_received_pay" ,
             "close_window" ,
             "{=PRev0037}I'm pleased.[if:convo_happy]" ,
             new ConversationSentence.OnConditionDelegate (this.barter_successful_condition) ,
             () => { base.AddLog (IssueSuccessText); } ,this ,100 ,null ,null ,null);

            dialog.AddDialogLine (
             "peasant_revenge_discuss_pr_demands_pl_pay_received_pay_received_pay_fail" ,
             "peasant_revenge_discuss_pr_demands_pl_pay_received_pay" ,
             "peasant_revenge_discuss_pr_demands_pl_options" ,
             "{=PRev0036}So, what is now?[ib:closed][if:idle_angry]" ,
             () => !this.barter_successful_condition ( ) ,
             null ,this ,100 ,null ,null ,null);

            return dialog;
        }
        #endregion

        #region player accuse hero persuation

        PersuasionTask _task;

        private DialogFlow GetPlayerAccuseHeroPersuasionDialogFlow ()
        {
            DialogFlow dialog = DialogFlow.CreateDialogFlow("peasant_revenge_player_accuse_start_persuasion",125);

            dialog.AddDialogLine (
                "peasant_revenge_player_accuse_started" ,
                "peasant_revenge_player_accuse_start_persuasion" ,
                "peasant_revenge_player_accuse_persuasion_start_reservation" ,
                "{=!}{PARTY_LEADER_START_PERSUASION_LINE}" ,
                new ConversationSentence.OnConditionDelegate (this.persuasion_start_captured_player_accuse_on_condition) ,
                new ConversationSentence.OnConsequenceDelegate (this.persuasion_start_captured_player_accuse_on_consequence) ,
                this ,100 ,null ,null ,null);

            dialog.AddDialogLine (
               "peasant_revenge_player_accuse_persuasion_rejected" ,
               "peasant_revenge_player_accuse_persuasion_start_reservation" ,
               "close_window" ,
               "{=!}{TRY_LATER_PERSUASION_LINE}" ,
               new ConversationSentence.OnConditionDelegate (this.persuasion_start_captured_player_accuse_persuasion_rejected_on_condition) ,
               () => {
                   this.persuasion_rejected_on_consequence ( );
                   leave_encounter_and_mission ( );
               } ,
               this ,100 ,null ,
               new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsMainHero) ,
               new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsPlayersCapturerHero));

            dialog.AddDialogLine (
                "peasant_revenge_player_accuse_persuasion_failed" ,
                "peasant_revenge_player_accuse_persuasion_start_reservation" ,
                "peasant_revenge_player_accuse_persuasion_ended" ,
                "{=!}{FAILED_PERSUASION_LINE}" ,
                new ConversationSentence.OnConditionDelegate (this.persuasion_failed_on_condition) ,
                new ConversationSentence.OnConsequenceDelegate (this.persuasion_failed_on_consequence) ,
                this ,100 ,null ,
                new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsMainHero) ,
                new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsPlayersCapturerHero));

            dialog.AddDialogLine (
                "peasant_revenge_player_accuse_persuasion_success" ,
                "peasant_revenge_player_accuse_persuasion_start_reservation" ,
                "peasant_revenge_player_accuse_persuasion_ended" ,
                "{=PRev0128}You're right." ,
                new ConversationSentence.OnConditionDelegate (ConversationManager.GetPersuasionProgressSatisfied) ,
                new ConversationSentence.OnConsequenceDelegate (this.persuasion_success_on_consequence) ,
                this ,int.MaxValue ,null ,
                new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsMainHero) ,
                new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsPlayersCapturerHero));

            dialog.AddDialogLine (
                "peasant_revenge_player_accuse_persuasion_attempt" ,
                "peasant_revenge_player_accuse_persuasion_start_reservation" ,
                "peasant_revenge_player_accuse_persuasion_select_option" ,
                "{=PRev0129}What's there to discuss?" ,
                () => { return persuade_not_failed_on_condition ( ); } ,
                null ,this ,10 ,null ,null ,null);

            #region OPTIONS           
            dialog.AddPlayerLine (
                    "peasant_revenge_player_accuse_persuasion_select_option_0" ,
                    "peasant_revenge_player_accuse_persuasion_select_option" ,
                    "peasant_revenge_player_accuse_persuasion_select_option_response" ,
                    "{=!}{REVENGER_PERSUADE_OPTION_0}" ,
                    () => { return this.persuasion_select_option_i_on_condition (0); } ,
                    () => { persuasion_select_option_i_on_consequence (0); } ,
                    this ,100 ,
                    (out TextObject hintText) => { return this.persuasion_clickable_option_i_on_condition (0 ,out hintText); } ,
                    () => { return this.persuasion_setup_option_i (0); } ,
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsMainHero) ,
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsPlayersCapturerHero));
            dialog.AddPlayerLine (
                    "peasant_revenge_player_accuse_persuasion_select_option_1" ,
                    "peasant_revenge_player_accuse_persuasion_select_option" ,
                    "peasant_revenge_player_accuse_persuasion_select_option_response" ,
                    "{=!}{REVENGER_PERSUADE_OPTION_1}" ,
                    () => { return this.persuasion_select_option_i_on_condition (1); } ,
                    () => { persuasion_select_option_i_on_consequence (1); } ,
                    this ,100 ,
                    (out TextObject hintText) => { return this.persuasion_clickable_option_i_on_condition (1 ,out hintText); } ,
                    () => { return this.persuasion_setup_option_i (1); } ,
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsMainHero) ,
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsPlayersCapturerHero));
            dialog.AddPlayerLine (
                    "peasant_revenge_player_accuse_persuasion_select_option_2" ,
                    "peasant_revenge_player_accuse_persuasion_select_option" ,
                    "peasant_revenge_player_accuse_persuasion_select_option_response" ,
                    "{=!}{REVENGER_PERSUADE_OPTION_2}" ,
                    () => { return this.persuasion_select_option_i_on_condition (2); } ,
                    () => { persuasion_select_option_i_on_consequence (2); } ,
                    this ,100 ,
                    (out TextObject hintText) => { return this.persuasion_clickable_option_i_on_condition (2 ,out hintText); } ,
                    () => { return this.persuasion_setup_option_i (2); } ,
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsMainHero) ,
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsPlayersCapturerHero));
            dialog.AddPlayerLine (
                   "peasant_revenge_player_accuse_persuasion_select_option_3" ,
                   "peasant_revenge_player_accuse_persuasion_select_option" ,
                   "peasant_revenge_player_accuse_persuasion_select_option_response" ,
                   "{=!}{REVENGER_PERSUADE_OPTION_3}" ,
                   () => { return this.persuasion_select_option_i_on_condition (3); } ,
                   () => { persuasion_select_option_i_on_consequence (3); } ,
                   this ,100 ,
                   (out TextObject hintText) => { return this.persuasion_clickable_option_i_on_condition (3 ,out hintText); } ,
                   () => { return this.persuasion_setup_option_i (3); } ,
                   new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsMainHero) ,
                   new ConversationSentence.OnMultipleConversationConsequenceDelegate (this.IsPlayersCapturerHero));
            #endregion
            //RESPONSE
            dialog.AddDialogLine (
                "peasant_revenge_player_accuse_persuasion_select_option_reaction" ,
                "peasant_revenge_player_accuse_persuasion_select_option_response" ,
                "peasant_revenge_player_accuse_persuasion_start_reservation" ,
                "{=*}{PERSUASION_REACTION}" ,
                new ConversationSentence.OnConditionDelegate (this.persuasion_selected_option_response_on_condition) ,
                new ConversationSentence.OnConsequenceDelegate (this.persuasion_selected_option_response_on_consequence) ,
                this ,100 ,null ,null ,null);

            return dialog;
        }

        private bool persuasion_selected_option_response_on_condition ()
        {
            PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs,PersuasionOptionResult>>().Item2;
            MBTextManager.SetTextVariable ("PERSUASION_REACTION" ,PersuasionHelper.GetDefaultPersuasionOptionReaction (item) ,false);
            if(item == PersuasionOptionResult.CriticalFailure)
            {
                this._task.BlockAllOptions ( );
            }
            return true;
        }

        private void persuasion_selected_option_response_on_consequence ()
        {
            Tuple<PersuasionOptionArgs,PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs,PersuasionOptionResult>>();
            float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Medium);
            float moveToNextStageChance;
            float blockRandomOptionChance;
            Campaign.Current.Models.PersuasionModel.GetEffectChances (tuple.Item1 ,out moveToNextStageChance ,out blockRandomOptionChance ,difficulty);
            this._task.ApplyEffects (moveToNextStageChance ,blockRandomOptionChance);
        }

        private bool persuasion_start_captured_player_accuse_on_condition ()
        {
            if(this._task.Options.Count > 0)
            {
                TextObject textObject = new TextObject("{=*}{COMMENT_LINE}",null);
                textObject.SetTextVariable ("COMMENT_LINE" ,new TextObject ("{=*}Is it true?[ib:closed][if:convo_thinking]" ,null));
                MBTextManager.SetTextVariable ("PARTY_LEADER_START_PERSUASION_LINE" ,textObject ,false);
                return true;
            }
            return false;
        }

        private bool persuasion_select_option_i_on_condition (int option_index)
        {
            if(this._task.Options.Count > 0)
            {
                TextObject textObject = new TextObject("{=*}{OPTION_LINE} {SUCCESS_CHANCE}",null);
                textObject.SetTextVariable ("SUCCESS_CHANCE" ,PersuasionHelper.ShowSuccess (this._task.Options.ElementAt (option_index) ,false));
                textObject.SetTextVariable ("OPTION_LINE" ,this._task.Options.ElementAt (option_index).Line);
                string option_string = $"REVENGER_PERSUADE_OPTION_{option_index}";
                MBTextManager.SetTextVariable (option_string ,textObject ,false);
                return true;
            }
            return false;
        }

        private void persuasion_select_option_i_on_consequence (int option_index)
        {
            if(this._task.Options.Count > 0)
            {
                this._task.Options [option_index].BlockTheOption (true);
            }
        }

        private bool persuasion_clickable_option_i_on_condition (int option_index ,out TextObject hintText)
        {
            hintText = new TextObject ("{=9ACJsI6S}Blocked" ,null);
            if(this._task.Options.Count > 0)
            {
                hintText = this._task.Options.ElementAt (option_index).IsBlocked ? hintText : TextObject.Empty;
                return !this._task.Options.ElementAt (option_index).IsBlocked;
            }
            return false;
        }

        private PersuasionOptionArgs persuasion_setup_option_i (int option_index)
        {
            return this._task.Options.ElementAt (option_index);
        }

        private void persuasion_success_on_consequence ()
        {
            pr_event_status = event_status.accusation_success;
            ConversationManager.EndPersuasion ( );
        }

        private void persuasion_failed_on_consequence ()
        {
            pr_event_status = event_status.accusation_fail;
            ConversationManager.EndPersuasion ( );
        }

        private void persuasion_rejected_on_consequence ()
        {
            pr_event_status = event_status.accusation_fail;
            ConversationManager.EndPersuasion ( );
        }

        private bool persuade_not_failed_on_condition ()
        {
            return !persuasion_failed_on_condition ( ) && !ConversationManager.GetPersuasionProgressSatisfied ( );
        }

        private bool persuasion_failed_on_condition ()
        {
            if(_task.Options.All ((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied ( ))
            {
                MBTextManager.SetTextVariable ("FAILED_PERSUASION_LINE" ,_task.FinalFailLine ,false);
                return true;
            }
            return false;
        }

        private bool persuasion_start_captured_player_accuse_persuasion_refuse_on_condition ()
        {
            return false;// TODO: Add correct reasons for refusal here.//get_party_leader_persuaded_count()> lordCanTryAsManyTimesToPersuadeThePartyLeader;
        }

        private bool persuasion_start_captured_player_accuse_persuasion_rejected_on_condition ()
        {
            if(persuasion_start_captured_player_accuse_persuasion_refuse_on_condition ( ))
            {
                MBTextManager.SetTextVariable ("TRY_LATER_PERSUASION_LINE" ,this._task.TryLaterLine ,false);
                return true;
            }
            return false;
        }

        private void persuasion_start_captured_player_accuse_on_consequence ()
        {
            ConversationManager.StartPersuasion (1f ,1f ,0f ,1f ,1f ,0f ,PersuasionDifficulty.Hard);
        }

        /// <summary>
        /// Getting the persuasion task.
        /// </summary>
        private void peasant_revenge_player_accuse_hero_consequence ()
        {
            int task_index = 0;
            _task = GetPersuasionTask (task_index);
            _task.UnblockAllOptions ( );
        }

        private TextObject GetAccuseLineByRelations (Hero hero ,Hero accusedhero ,Hero accuser)
        {
            TextObject text = new TextObject("{=*}{ACCUSE_LINE_BY_RELATIONS}");




            return text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="trait"></param>
        /// <returns></returns>
        private TextObject GetAccuseLineByTrait (Hero hero ,TraitObject trait)
        {
            TextObject text = new TextObject("{=*}{ACCUSE_LINE_BY_TRAIT}");

            //TODO: find the major traits and select the string lines.
            //TODO: selection of lines should depend on player's knowledge about the hero one is accusing. 

            CharacterTraits traits = hero.GetHeroTraits();
            //List<int> trait_list = new List<int>();
            //trait_list.Add(traits.Calculating);
            //trait_list.Add(traits.Valor);
            //trait_list.Add(traits.Honor);
            //trait_list.Add(traits.Mercy);
            //trait_list.Add(traits.Generosity);
            //trait_list.Sort();

            if(trait == DefaultTraits.Honor)
            {
                if(traits.Honor > 0)
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME} thinks only how much honor {?ACCUSEDHERO.GENDER}she{?}he{\\?} will get from looting." ,null);
                }
                else
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME}'s disrespect and disobedience is a shame to all." ,null);
                }
            }

            if(trait == DefaultTraits.Valor)
            {
                if(traits.Valor > 0)
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME} courage have led us to this tragedy." ,null);
                }
                else
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME} is brave only when fighting the peasants." ,null);
                }
            }

            if(trait == DefaultTraits.Mercy)
            {
                if(traits.Mercy > 0)
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME} have not showed kindness to poor peasants." ,null);
                }
                else
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME} is a cruel and bloodthirsty bastard." ,null);
                }
            }

            if(trait == DefaultTraits.Generosity)
            {
                if(traits.Generosity > 0)
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME} plundered the countryside to get charity for {?ACCUSEDHERO.GENDER}her{?}his{\\?} peasants." ,null);
                }
                else
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME} is selfish and greedy crook." ,null);
                }
            }

            if(trait == DefaultTraits.Calculating)
            {
                if(traits.Calculating > 0)
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME} planed all the looting." ,null);
                }
                else
                {
                    text = new TextObject ("{=*}{ACCUSEDHERO.NAME}'s crazy decisions took many souls." ,null);
                }
            }

            StringHelpers.SetCharacterProperties ("ACCUSEDHERO" ,hero.CharacterObject ,text ,false);

            return text;
        }

        private PersuasionTask GetPersuasionTask (int task_index)
        {
            PersuasionTask persuasionTask = new PersuasionTask(0);

            persuasionTask.FinalFailLine = new TextObject ("{=PRev0131}I think...[ib:thinking]" ,null);
            persuasionTask.TryLaterLine = new TextObject ("{=PRev0078}I do not have time to talk right now.[rf:idle_angry][ib:closed][if:idle_angry]" ,null);
            persuasionTask.SpokenLine = new TextObject ("{=PRev0130}Maybe..." ,null);

            if(task_index == 0)
            {
                ///         * Target hero blame accused hero traits.
                ///         * Target hero blame accused hero relations with raided village clan.
                ///         * Target hero blame accused hero relations with raided notable.
                ///         * Target hero blame the clan/kingdom decision.
                ///         * Try sound convincing.

                List<PeasantRevengeConfiguration.TraitAndValue> testtraits = new List<PeasantRevengeConfiguration.TraitAndValue>();

                PersuasionOptionArgs option0 = new PersuasionOptionArgs(DefaultSkills.Leadership,DefaultTraits.Valor,TraitEffect.Positive,
                        GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,testtraits),
                        false,GetAccuseLineByTrait(Hero.OneToOneConversationHero,DefaultTraits.Valor),null,false,false,false);
                persuasionTask.AddOptionToTask (option0);

                PersuasionOptionArgs option1 = new PersuasionOptionArgs(DefaultSkills.Charm,DefaultTraits.Mercy,TraitEffect.Positive,
                        GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,testtraits),
                        false,GetAccuseLineByTrait(Hero.OneToOneConversationHero,DefaultTraits.Mercy),null,false,false,false);
                persuasionTask.AddOptionToTask (option1);

                PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm,DefaultTraits.Honor,TraitEffect.Negative,
                        GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,testtraits),
                        false,GetAccuseLineByTrait(Hero.OneToOneConversationHero,DefaultTraits.Honor),null,false,false,false);
                persuasionTask.AddOptionToTask (option2);

                PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm,DefaultTraits.Generosity,TraitEffect.Positive,
                        GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,testtraits),
                        false,GetAccuseLineByTrait(Hero.OneToOneConversationHero,DefaultTraits.Generosity),null,false,false,false);
                persuasionTask.AddOptionToTask (option3);

                PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Charm,DefaultTraits.Calculating,TraitEffect.Positive,
                        GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,testtraits),
                        false,GetAccuseLineByTrait(Hero.OneToOneConversationHero,DefaultTraits.Calculating),null,false,false,false);
                persuasionTask.AddOptionToTask (option4);
            }

            return persuasionTask;
        }

        private PersuasionArgumentStrength GetPersuationArgumentStrength (CharacterObject target_hero ,List<PeasantRevengeConfiguration.TraitAndValue> traits_and_values)
        {
            int valor = 0, mercy = 0, honor = 0, generosity = 0, calculating = 0;

            foreach(PeasantRevengeConfiguration.TraitAndValue tv in traits_and_values)
            {
                switch(tv.trait)
                {
                    case "Valor":
                        valor = tv.value;
                        break;
                    case "Mercy":
                        mercy = tv.value;
                        break;
                    case "Honor":
                        honor = tv.value;
                        break;
                    case "Generosity":
                        generosity = tv.value;
                        break;
                    case "Calculating":
                        calculating = tv.value;
                        break;
                    default:
                        break;
                }
            }

            Tuple<TraitObject,int> [] traitCorrelations = this.GetTraitCorrelations(valor,mercy,honor,generosity,calculating);
            PersuasionArgumentStrength argstr = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(target_hero,traitCorrelations); // how much argument trait tuple correlates with npc and player  

            PersuasionDifficulty min_difficulty = PersuasionDifficulty.Medium;

            PersuasionDifficulty difficulty = GetStartPersuasionDifficulty(Hero.MainHero,target_hero.HeroObject,min_difficulty);

            argstr = argstr - (difficulty - min_difficulty);

            if(argstr < PersuasionArgumentStrength.ExtremelyHard)
            {
                argstr = PersuasionArgumentStrength.ExtremelyHard;
            }
            else if(argstr > PersuasionArgumentStrength.ExtremelyEasy)
            {
                argstr = PersuasionArgumentStrength.ExtremelyEasy;
            }

            return argstr;
        }

        private Tuple<TraitObject ,int> [] GetTraitCorrelations (int valor = 0 ,int mercy = 0 ,int honor = 0 ,int generosity = 0 ,int calculating = 0)
        {
            return new Tuple<TraitObject ,int> []
            {
                new Tuple<TraitObject, int>(DefaultTraits.Valor, valor),
                new Tuple<TraitObject, int>(DefaultTraits.Mercy, mercy),
                new Tuple<TraitObject, int>(DefaultTraits.Honor, honor),
                new Tuple<TraitObject, int>(DefaultTraits.Generosity, generosity),
                new Tuple<TraitObject, int>(DefaultTraits.Calculating, calculating)
            };
        }

        private PersuasionDifficulty GetStartPersuasionDifficulty (Hero hero_initiator ,Hero hero_target ,PersuasionDifficulty min_difficulty)
        {
            PersuasionDifficulty diff = min_difficulty;

            //TODO: add traits.
            bool have_traits = CfgParser.hero_trait_list_condition(hero_initiator,"",out string parse_error,hero_target);

            if(!have_traits)
            {
                diff += 1;
            }

            if(hero_initiator.MapFaction != hero_target.MapFaction)
            {
                diff += 1;
            }

            if(hero_initiator.MapFaction.IsAtWarWith (hero_target.MapFaction))
            {
                diff += 1;
            }

            int relation = hero_target.GetRelation(hero_initiator);

            if(relation < 0)
            {
                diff += 1;
            }
            else if(relation > 20)
            {
                diff -= 1;
            }

            if(hero_target.IsEnemy (hero_initiator))
            {
                diff += 1;
            }
            else if(hero_target.IsFriend (hero_initiator))
            {
                diff -= 1;
            }

            if(diff > PersuasionDifficulty.Impossible)
            {
                diff = PersuasionDifficulty.Impossible;
            }

            return diff;
        }

        private bool IsPlayersCapturerHero (IAgent agent)
        {
            if(Hero.OneToOneConversationHero != null &&
                Hero.OneToOneConversationHero.PartyBelongedToAsPrisoner != null &&
                Hero.OneToOneConversationHero == Hero.OneToOneConversationHero.PartyBelongedToAsPrisoner.LeaderHero)
                return false;
            return agent.Character == Hero.OneToOneConversationHero.CharacterObject;
        }

        private bool IsNotableHero (IAgent agent)
        {
            if(!(Hero.OneToOneConversationHero != null && (Hero.OneToOneConversationHero.IsHeadman || Hero.OneToOneConversationHero.IsRuralNotable)))
                return false;
            return agent.Character == Hero.OneToOneConversationHero.CharacterObject;
        }

        private bool IsMainHero (IAgent agent)
        {
            return agent.Character == CharacterObject.PlayerCharacter;
        }

        private void leave_encounter_and_mission ()
        {
            if(PlayerEncounter.Current == null)
                return;
            PlayerEncounter.LeaveEncounter = true;
            if(PlayerEncounter.InsideSettlement)
                if(CampaignMission.Current != null)
                    CampaignMission.Current.EndMission ( );
        }

        #endregion

        private Hero get_first_companion ()
        {
            TroopRoster troopsLordParty = MobileParty.MainParty.MemberRoster;
            for(int j = 0;j < troopsLordParty.Count;j++)
            {
                CharacterObject troop = troopsLordParty.GetCharacterAtIndex(j);
                if(troop.IsHero && !troop.IsPlayerCharacter)
                {
                    return troop.HeroObject;
                }
            }
            return null;
        }

        private bool peasant_revenge_get_executioner_companion_condition ()
        {
            Hero companion = get_first_companion();
            if(companion != null)
            {
                StringHelpers.SetCharacterProperties ("EXECUTIONER" ,companion.CharacterObject);
                return true;
            }
            return false;
        }

        private bool peasant_revenge_criminal_has_suporters_clickable_condition (out TextObject textObject)
        {
            List<Hero> saver = GetHeroSuportersWhoCouldPayUnpaidRansom(this._targetHero,_get_reparation_value());
            bool start = !saver.IsEmpty();
            if(saver.Count ( ) == 1)
            {
                textObject = new TextObject ("{=PRev0066}{SAVER.NAME} will support you.");
                StringHelpers.SetCharacterProperties ("SAVER" ,saver.First ( ).CharacterObject ,textObject ,false);
            }
            else
            {
                textObject = new TextObject ("{=PRev0067}{SUPPORTERCOUNT} heroes can support you.");
                MBTextManager.SetTextVariable ("SUPPORTERCOUNT" ,(float)saver.Count ( ));
            }

            return start;
        }

        private List<Hero> GetHeroSuportersWhoCouldPayUnpaidRansom (Hero hero ,int goldNeeded)
        {
            List<Hero> list = new List<Hero>();

            float criminalHeroFromClanSuporterMinimumAge = 5f;
            int criminalHeroFromClanSuporterMinimumRelation = -90;

            if(hero.Clan != null)
            {
                list.AddRange (hero.Clan.Heroes.Where ((x) =>
                 x != hero &&
                 x.Gold >= goldNeeded &&
                 x.IsAlive &&
                 x.Age >= criminalHeroFromClanSuporterMinimumAge && !x.IsEnemy (hero) &&
                 x.GetRelation (hero) >= criminalHeroFromClanSuporterMinimumRelation &&
                (// if not relative, friend or clan leader                 
                  x.Children.Contains (hero) || hero.Children.Contains (x) || (x.IsFriend (hero)) // TODO: What about other relatives?
                )).ToList ( ));
            }

            return list;
        }

        Hero GetHeroSupporter (Hero hero ,int goldNeeded ,int hero_index)
        {
            List<Hero> hero_list = GetHeroSuportersWhoCouldPayUnpaidRansom(hero,goldNeeded);

            if(hero_list.Count > hero_index)
            {
                return hero_list [hero_index];
            }
            return null;
        }

        bool _hero_has_supporter_condition (Hero hero ,int goldNeeded ,int index)
        {
            Hero hero_supporter = GetHeroSupporter(hero,goldNeeded,index);
            if(hero_supporter == null)
            {
                return false;
            }

            StringHelpers.SetCharacterProperties ($"SAVER{index}" ,hero_supporter.CharacterObject);
            return true;
        }

        bool hero_would_accept_reparation_from_others_instead_of_criminal (Hero hero ,Hero criminal)
        {
            bool reparation_is_ccepted =
                   (hero.GetTraitLevel(DefaultTraits.Honor)<0||hero.IsHumanPlayerCharacter) && hero.GetRelation(criminal) >= -10;

            if(reparation_is_ccepted)
            {
                /*TODO: variations, when accepted the payment*/
            }
            else
            {
                if(hero.GetTraitLevel (DefaultTraits.Honor) > 0)
                {
                    MBTextManager.SetTextVariable ("PL_PAY_INSTEAD_OF_CR" ,
                    new TextObject ("{=*}No, I'll not accept your proposal. Criminal must pay the price.[if:convo_furious][ib:warrior]") ,false);
                }
                else if(hero.GetTraitLevel (DefaultTraits.Calculating) > 0)
                {
                    MBTextManager.SetTextVariable ("PL_PAY_INSTEAD_OF_CR" ,
                    new TextObject ("{=*}No, I must be sure the raiding is stopped.[if:convo_thinking]") ,false);
                }
                else
                {
                    MBTextManager.SetTextVariable ("PL_PAY_INSTEAD_OF_CR" ,
                   new TextObject ("{=*}No, I'll not accept your proposal.[if:idle_angry][ib:convo_closed]") ,false);
                }
            }

            return reparation_is_ccepted;
        }

        /// <summary>
        /// Returns prisoner from party where hero is inprisoned
        /// </summary>
        /// <param name="hero">blamer hero</param>
        /// <returns></returns>
        private Hero get_prisoner_blamed (Hero hero ,int index)
        {
            if(hero == null
                || hero.PartyBelongedToAsPrisoner == null
                || hero.PartyBelongedToAsPrisoner.PrisonerHeroes == null
                || hero.PartyBelongedToAsPrisoner.PrisonerHeroes.IsEmpty ( ))
                return null;

            bool criminalWillBlameOtherLordForTheCrime =
                    hero.GetTraitLevel(DefaultTraits.Honor)<=0 || hero.IsHumanPlayerCharacter;

            var prisoners = hero.PartyBelongedToAsPrisoner.PrisonerHeroes.Where((x) =>
                  x!=null&&
                  x.HeroObject!=null&&
                  x.HeroObject.Clan!=null&&
                  !x.HeroObject.Clan.IsAtWarWith(hero.Clan)&& /*TODO: lets allow to blame */
                  x.HeroObject!=hero&&
                  criminalWillBlameOtherLordForTheCrime&&
                  (x.HeroObject.Clan==hero.Clan||x.HeroObject.Clan.Kingdom==hero.Clan.Kingdom));

            if(prisoners == null || prisoners.IsEmpty ( )) // if empty , try to blame some random prisoner
            {
                prisoners = hero.PartyBelongedToAsPrisoner.PrisonerHeroes.Where ((x) =>
                x != null &&
                x.HeroObject != null &&
                x.HeroObject.Clan != null &&
                x.HeroObject != hero &&
                criminalWillBlameOtherLordForTheCrime);
            }

            if(prisoners != null && !prisoners.IsEmpty ( ))
            {
                if(prisoners.Count ( ) > index)
                {
                    return prisoners.ElementAt (index).HeroObject;
                }
            }
            return null;
        }

        bool _hero_can_accuse_condition (Hero hero ,int index)
        {
            Hero hero_accused = get_prisoner_blamed(hero,index);
            if(hero_accused == null)
            {
                return false;
            }

            StringHelpers.SetCharacterProperties ($"accuseD{index}" ,hero_accused.CharacterObject);
            return true;
        }


        private Hero get_any_prisoner_to_be_blamed (Hero hero ,Hero prisoner ,int index)
        {
            if(hero == null || hero.PartyBelongedTo == null || hero.PartyBelongedTo.Party == null)
                return null;

            var prisoners = hero.PartyBelongedTo.Party.PrisonerHeroes.Where((x) =>
                 x!=null&&
                 x.HeroObject!=null&&
                 x.HeroObject.Clan!=null&&
                 x.HeroObject != prisoner);

            if(prisoners != null && !prisoners.IsEmpty ( ))
            {
                if(prisoners.Count ( ) > index)
                {
                    return prisoners.ElementAt (index).HeroObject;
                }
            }
            return null;
        }

        bool _hero_can_accuse_prisoner_condition (Hero hero ,Hero prisoner ,int index)
        {
            Hero hero_accused = get_any_prisoner_to_be_blamed(hero,prisoner,index);
            if(hero_accused == null)
            {
                return false;
            }

            StringHelpers.SetCharacterProperties ($"accuseD{index}" ,hero_accused.CharacterObject);
            return true;
        }


        private bool _hero_has_enougth_gold_for_reparation_condition ()
        {
            int reparation = _get_reparation_value();
            MBTextManager.SetTextVariable ("REPARATION" ,reparation);
            return this._targetHero.Gold >= reparation;
        }

        private bool _hero_reparation_condition ()
        {
            MBTextManager.SetTextVariable ("REPARATION" ,_get_reparation_value ( ));
            return true;
        }

        private Hero _get_victim ()
        {
            return this._targetHero != null ? this._targetHero : null;
        }

        private bool _hero_will_not_pay_reparation_on_clickable (Hero requester ,Hero leader ,out TextObject text)
        {

            bool party_let_revenge_con = _will_party_leader_let_the_criminal_die(requester,leader);
            text = TextObject.Empty;

            if(party_let_revenge_con)
            {
                text = new TextObject ("{=*}Certain death");
            }
            else
            {
                text = new TextObject ("{=*}You are saved");
            }

            return true;
        }

        private bool _hero_will_not_pay_reparation_on_condition (Hero requester ,Hero leader)
        {
            if(leader == null || requester == null)
                return false;

            bool party_let_revenge_con = _will_party_leader_let_the_criminal_die(requester,leader);

            if(party_let_revenge_con)
            {
                MBTextManager.SetTextVariable ("PL_NOT_PAY_LEADER_ANSW" ,
                    new TextObject ("{=PRev0063}Peasant will have your head.[if:convo_thinking][rf:convo_grave][ib:closed]") ,false);
            }
            else
            {
                MBTextManager.SetTextVariable ("PL_NOT_PAY_LEADER_ANSW" ,
                    new TextObject ("{=PRev0064}You will be fine.[if:convo_happy][if:convo_thinking][ib:closed]") ,false);
            }
            return true;
        }

        private bool _will_party_leader_let_the_criminal_die (Hero requester ,Hero leader)//MBRandom.RandomInt(10) > 5; //TODO: Fix me
        {
            // bool party_relatives_with_criminal_condition = (currentRevenge.party.Owner.Children.Contains(currentRevenge.criminal.HeroObject)||
            //currentRevenge.criminal.HeroObject.Children.Contains(currentRevenge.party.Owner))&&
            //                                          CheckConditions(currentRevenge.party.Owner,currentRevenge.criminal.HeroObject,
            //                                          _cfg.values.ai.lordIfRelativesWillHelpTheCriminal);
            // bool party_help_criminal_con = CheckConditions(currentRevenge.party.Owner,
            //     currentRevenge.executioner.HeroObject,_cfg.values.ai.lordWillAffordToHelpTheCriminalEnemy);
            // bool party_friend_to_criminal_con = currentRevenge.party.Owner.IsFriend(currentRevenge.criminal.HeroObject);
            // bool party_overide_con = CheckConditions(currentRevenge.party.Owner,
            //     currentRevenge.executioner.HeroObject,_cfg.values.ai.partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet)||
            //     currentRevenge.party.Owner.IsFriend(currentRevenge.executioner.HeroObject);
            // bool party_let_revenge_con = (!party_help_criminal_con&&!party_friend_to_criminal_con&&!party_relatives_with_criminal_condition)||party_overide_con;

            // return party_let_revenge_con;



            return leader.GetTraitLevel (DefaultTraits.Mercy) < 0;
        }

        private void _pay_reparation (Hero sender ,Hero receiver)
        {
            GiveGoldAction.ApplyBetweenCharacters (sender ,receiver ,_get_reparation_value ( ) ,false);
        }

        private void ExecuteHero (Hero executioner ,Hero victim)
        {
            if(victim != Hero.MainHero)
            {
                MBInformationManager.ShowSceneNotification (HeroExecutionSceneNotificationData.CreateForInformingPlayer (
                    executioner ,victim ,SceneNotificationData.RelevantContextType.Map));
                KillCharacterAction.ApplyByExecution (victim ,executioner ,true ,true);
            }
            else
            {
                KillCharacterAction.ApplyByExecution (victim ,executioner ,true ,false);
            }

        }

        private void _disband_quest_giver_party ()
        {
            if(base.QuestGiver.PartyBelongedTo != null)
            {
                if(base.QuestGiver.PartyBelongedTo.MapEvent == null) // crash during battle update map event, if not checked
                {
                    DestroyPartyAction.ApplyForDisbanding (base.QuestGiver.PartyBelongedTo ,base.QuestGiver.HomeSettlement);
                }
            }
        }

        private void CompleteQuestWithSuccessConsequences ()
        {
            _disband_quest_giver_party ( );
            base.CompleteQuestWithSuccess ( );
        }

        private void CompleteQuestWithFailConsequences ()
        {
            _disband_quest_giver_party ( );
            base.CompleteQuestWithFail ( );
        }

        private void CompleteQuestWithCancelConsequences ()
        {
            _disband_quest_giver_party ( );
            base.CompleteQuestWithCancel ( );
        }

        private void CompleteQuestWithBetrayConsequences ()
        {
            _disband_quest_giver_party ( );
            base.CompleteQuestWithBetrayal ( );
        }

        #region REPARATION BARTER
        private bool barter_successful_condition ()
        {
            return Campaign.Current.BarterManager.LastBarterIsAccepted;
        }

        public bool InitializeReparationsBarterableBarterContext (Barterable barterable ,BarterData args ,object obj)
        {
            return barterable.GetType ( ) == typeof (ReparationsBarterable) && barterable.OriginalOwner == Hero.OneToOneConversationHero;
        }

        private void _player_barter_consequence ()
        {
            if(Hero.OneToOneConversationHero.PartyBelongedTo == null)
            {
                System.Diagnostics.Debug.WriteLine ($"Cannot barter.");
                return; //Cannot use, when other party is null.
            }
            List<Barterable> barterables = new List<Barterable>();
            barterables.Add (new ReparationsBarterable (Hero.OneToOneConversationHero ,PartyBase.MainParty ,null ,Hero.MainHero ,_get_reparation_value ( )));
            BarterManager instance = BarterManager.Instance;
            instance.StartBarterOffer (
                Hero.MainHero ,
                Hero.OneToOneConversationHero ,
                PartyBase.MainParty ,
                Hero.OneToOneConversationHero.PartyBelongedTo.Party ,null ,
                new BarterManager.BarterContextInitializer (InitializeReparationsBarterableBarterContext) ,0 ,false ,barterables);
        }
        #endregion
    }

}
