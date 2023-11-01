using Helpers;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace PeasantRevenge
{
#pragma warning disable IDE1006 // Naming Styles
    internal class PeasantRevengeBehavior : CampaignBehaviorBase
    {
        bool revengerPartiesCleanUp = true;
        string revengerPartyNameStart = "Revenger_";
        public PeasantRevengeModCfg _cfg = new PeasantRevengeModCfg();

        List<PeasantRevengeData> revengeData = new List<PeasantRevengeData>();

        PeasantRevengeData currentRevenge = new PeasantRevengeData();
        public class PersuadedHeroData
        {

            public string Id = ""; // hero id
            public uint persuade_try_count = 0;
        }
        #region notable persuade TODO: someday move it to quest
        public enum persuade_type
        {
            none,
            bribe,
            teach,
            show_example_success,
            show_example_fail,
            bribe_success,
            bribe_fail,
        }

        persuade_type persuade_status = persuade_type.none;
        bool previous_can_revenge = false;
        #endregion

        public List<PersuadedHeroData> persuadedHeroData = new List<PersuadedHeroData>();

        internal class PeasantRevengeData
        {
            public enum quest_state
            {
                none,
                ready,
                begin,
                start,
                stop,
                clear
            }

            public enum quest_result
            {
                none,
                village_denied, // village has no appropriate notable peasant
                party_denied,
                clan_denied,
                kingdom_denied,
                party_no_decision,
                clan_leader_no_decision,
                saver_no_decision,
                notable_interrupted, // when notable party is stopped by other hero
                criminal_paid,
                party_paid,
                clan_paid,
                kingdom_paid,
                criminal_killed,
                accused_hero_killed,
                notable_killed,
                messenger_killed,
                accused_hero_paid,
                ransom_paid_to_party,
                ransom_not_paid_to_party,
            }

            public List<quest_result> quest_Results = new List<quest_result>();

            public Village village;
            public CharacterObject criminal;
            public CharacterObject accused_hero; // it is hero who was blamed for the crime by criminal
            public PartyBase party; //party what arrested the criminal

            public CharacterObject executioner;
            public PartyBase nobleParty; //
            public MobileParty xParty; //
            public CharacterObject targetHero;
            public int reparation;
            public quest_state state = quest_state.none;
            public CampaignTime startTime;
            public CampaignTime dueTime;

            private bool can_peasant_revenge_lord_start = false;
            private bool can_peasant_revenge_peasant_start = false;
            private bool can_peasant_revenge_messenger_peasant_start = false;
            private bool can_peasant_revenge_support_lord_start = false;
            private bool can_peasant_revenge_accuser_lord_start = false;
            private bool can_peasant_revenge_peasant_finish_start = false;

            public bool Can_peasant_revenge_lord_start
            {
                get
                {
                    bool value = state == quest_state.start && nobleParty == null && party.PrisonerHeroes.Contains(Hero.MainHero.CharacterObject) && criminal.HeroObject.IsAlive && party.LeaderHero != null; // will start if hero leader null - means karavan party
                    return value;
                }

                private set => can_peasant_revenge_lord_start = value;
            }

            public bool Can_peasant_revenge_peasant_start
            {
                get
                {
                    bool value = state == quest_state.start && nobleParty == null && PartyBase.MainParty.PrisonerHeroes.Contains(criminal) && criminal.HeroObject.IsAlive;
                    return value;
                }
                private set => can_peasant_revenge_peasant_start = value;
            }

            public bool Can_peasant_revenge_messenger_peasant_start
            {
                get
                {
                    bool value = state == quest_state.start && nobleParty != null && !party.PrisonerHeroes.Contains(Hero.MainHero.CharacterObject) && criminal.HeroObject.IsAlive && criminal.HeroObject.IsPrisoner && !Hero.MainHero.IsPrisoner;
                    return value;
                }
                private set => can_peasant_revenge_messenger_peasant_start = value;
            }

            public bool Can_peasant_revenge_support_lord_start { get => can_peasant_revenge_support_lord_start; set => can_peasant_revenge_support_lord_start = value; }
            public bool Can_peasant_revenge_accuser_lord_start { get => can_peasant_revenge_accuser_lord_start; set => can_peasant_revenge_accuser_lord_start = value; }
            public bool Can_peasant_revenge_peasant_finish_start { get => can_peasant_revenge_peasant_finish_start; set => can_peasant_revenge_peasant_finish_start = value; }

            public void Stop()
            { 
                if (state != quest_state.clear)
                {
                    state = quest_state.stop;
                }
            }

            public void Ready()
            {
                if (state == quest_state.none)
                {
                    state = quest_state.ready;
                }
            }

            public void Start()
            {
                if (state == quest_state.begin)
                {
                    state = quest_state.start;
                }
            }

            public void Begin()
            {
                if (state == quest_state.ready)
                {
                    state = quest_state.begin;
                }
            }

            public void Clear()
            {
                state = quest_state.clear; // make sure to set clear only when is impossible to continue the quest             
            }
        }

        public override void SyncData(IDataStore dataStore)
        {

        }

        public override void RegisterEvents()
        {
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, HeroPrisonerTaken);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, HeroPrisonerReleased);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoadedEvent);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreatedEvent);
            CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, VillageBeingRaided);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickEvent);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, HeroKilledEvent);
            CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, OnPartyDisbandedEvent);
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, OnPartyRemovedEvent);
        }

        private void OnNewGameCreatedEvent(CampaignGameStarter campaignGameStarter)
        {
            LoadConfiguration(campaignGameStarter);
            AddGameMenus(campaignGameStarter);
        }

        public PeasantRevengeConfiguration CheckModules(PeasantRevengeConfiguration cfg_source)
        {
            string[] moduleNames = Utilities.GetModulesNames();

            foreach (string modulesId in moduleNames)
            {
                if (modulesId.Contains("Bannerlord.Diplomacy")) // Diplomacy mod patch
                {
                    cfg_source.allowLordToKillMessenger = false;
                    cfg_source.allowPeasantToKillLord = false;
                    break;
                }
            }

            return cfg_source;
        }

        #region Help village menu


        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "join_encounter",
                "join_encounter_help_defenders_force",
                "{=PRev0087}Declare war to {KINGDOM}, and help {DEFENDER}.",
                new GameMenuOption.OnConditionDelegate(this.game_menu_join_encounter_help_defenders_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_encounter_help_defenders_on_consequence),
                false, -1, false, null);
        }

        private bool game_menu_join_encounter_help_defenders_on_condition(MenuCallbackArgs args)
        {
            if (!_cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu) return false;

            args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
            MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
            IFaction mapFactionAttacker = encounteredBattle.GetLeaderParty(BattleSideEnum.Attacker).MapFaction;
            //IFaction mapFactionDefender = encounteredBattle.GetLeaderParty(BattleSideEnum.Defender).MapFaction;

            bool canStartHelpVillageMenu = encounteredBattle.MapEventSettlement != null &&
                !mapFactionAttacker.IsAtWarWith(MobileParty.MainParty.MapFaction) &&
                //!mapFactionDefender.IsAtWarWith(MobileParty.MainParty.MapFaction) &&
                mapFactionAttacker != MobileParty.MainParty.MapFaction && // if removed can attack own party (not for this mod)
                encounteredBattle.MapEventSettlement.IsVillage &&
                encounteredBattle.MapEventSettlement.IsUnderRaid;

            if (canStartHelpVillageMenu)
            {
                MBTextManager.SetTextVariable("KINGDOM", mapFactionAttacker.Name.ToString());
                if (mapFactionAttacker.NotAttackableByPlayerUntilTime.IsFuture)
                {
                    args.IsEnabled = false;
                    args.Tooltip = GameTexts.FindText("str_enemy_not_attackable_tooltip", null);
                }
            }

            return canStartHelpVillageMenu;
        }
        private void game_menu_join_encounter_help_defenders_on_consequence(MenuCallbackArgs args)
        {
            MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
            IFaction mapFactionAttacker = encounteredBattle.GetLeaderParty(BattleSideEnum.Attacker).MapFaction;
            IFaction mapFactionDefender = encounteredBattle.GetLeaderParty(BattleSideEnum.Defender).MapFaction;

            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;

            if (!mapFactionAttacker.IsAtWarWith(MobileParty.MainParty.MapFaction))
            {
                BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, encounteredBattle.GetLeaderParty(BattleSideEnum.Attacker));
                //if (MobileParty.MainParty.MapFaction == mapFactionAttacker)
                //{
                //    ChangeCrimeRatingAction.Apply(MobileParty.MainParty.MapFaction, 61f);
                //}
            }

            if (((encounteredParty != null) ? encounteredParty.MapEvent : null) != null)
            {
                PlayerEncounter.JoinBattle(BattleSideEnum.Defender);
                GameMenu.ActivateGameMenu("encounter");
                if (!mapFactionDefender.IsAtWarWith(MobileParty.MainParty.MapFaction))
                {
                    TextObject menuText = new TextObject("{=PRev0086}You decided to...");
                    MBTextManager.SetTextVariable("ENCOUNTER_TEXT", menuText, true);
                }
                return;
            }
        }

        #endregion
        private void OnPartyDisbandedEvent(MobileParty party, Settlement settlement)
        {
            OnAnyRevengePartyIsRemoved(party.Party);
            OnAnyCapturerPartyIsRemoved(party.Party);
        }

        private void OnPartyRemovedEvent(PartyBase party)
        {
            OnAnyRevengePartyIsRemoved(party);
            OnAnyCapturerPartyIsRemoved(party);
        }

        private void OnAnyRevengePartyIsRemoved(PartyBase party)
        {
            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) => (
            x.xParty != null &&
            ((x.xParty.Party?.Id.ToString().Equals(party?.Id.ToString())) ?? false)));

            if (currentData != null && !currentData.IsEmpty())
            {
                foreach (PeasantRevengeData revenge in currentData)
                {
                    revenge.Clear();
                }
            }
        }

        private void OnAnyCapturerPartyIsRemoved(PartyBase party)
        {
            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) => (
            ((x.party?.Id.ToString().Equals(party?.Id.ToString())) ?? false)));

            if (currentData != null && !currentData.IsEmpty())
            {
                foreach (PeasantRevengeData revenge in currentData)
                {
                    revenge.Clear();
                }
            }
        }

        private void HeroKilledEvent(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) =>
            (x.criminal == victim.CharacterObject ||
             x.targetHero == victim.CharacterObject ||
             x.executioner == victim.CharacterObject
            ));

            if (currentData != null && !currentData.IsEmpty())
            {
                foreach (PeasantRevengeData revenge in currentData)
                {
                    revenge.Stop();
                }
            }
        }

        private void VillageBeingRaided(Village village)
        {
            if (village.Settlement.LastAttackerParty.Party.LeaderHero == null) return;

            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) => (
            x.criminal == village.Settlement.LastAttackerParty.Party.LeaderHero.CharacterObject &&
            x.village == village));

            if (currentData.IsEmpty())
            {
                lock (revengeData)
                {
                    revengeData.Add(new PeasantRevengeData
                    {
                        village = village,
                        criminal = village.Settlement.LastAttackerParty.Party.LeaderHero.CharacterObject,
                        dueTime = CampaignTime.DaysFromNow(_cfg.values.peasantRevengeTimeoutInDays)
                    });
                }
            }
        }

        private void HeroPrisonerTaken(PartyBase party, Hero prisoner)
        {
            if (party.Owner == null) return;
            if (party.LeaderHero == null) return;
            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) => (x.criminal == prisoner.CharacterObject));

            if (currentData != null && !currentData.IsEmpty())
            {
                foreach (PeasantRevengeData revenge in currentData)
                {
                    if (revenge.criminal.HeroObject == prisoner && revenge.executioner == null)
                    {
                        CharacterObject executioner = GetRevengeNotable(revenge.village.Settlement);

                        if (executioner != null)
                        {
                            revenge.executioner = executioner;
                            revenge.reparation = (int)(revenge.village.Hearth * _cfg.values.ReparationsScaleToSettlementHearts);
                            revenge.party = party;
                            revenge.dueTime = CampaignTime.DaysFromNow(_cfg.values.peasantRevengeTimeoutInDays);
                            revenge.targetHero = party.LeaderHero.CharacterObject;
                            revenge.startTime = CampaignTime.DaysFromNow(_cfg.values.peasantRevengeSartTimeInDays);
                            revenge.Ready();
                        }
                        else
                        {
                            revenge.Stop();
                        }
                    }
                }
            }
            
            currentData = revengeData.Where((x) => ((x.targetHero == prisoner.CharacterObject) || (x.executioner == prisoner.CharacterObject)));

            if (currentData != null && !currentData.IsEmpty())
            {
                foreach (PeasantRevengeData revenge in currentData)
                {
                    revenge.Stop();
                }
            }
        }

        private void HeroPrisonerReleased(Hero criminal, PartyBase party, IFaction faction, EndCaptivityDetail detail)
        {
            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) => 
            (x.criminal == criminal.CharacterObject));
            
            if (currentData != null && !currentData.IsEmpty())
            {
                foreach (PeasantRevengeData revenge in currentData)
                {
                    revenge.Stop();
                }
            }
        }

        private void HourlyTickEvent()
        {

            if (revengerPartiesCleanUp) // clean spawned parties after load (because we do not save revenge data - revenger party is unusable)
            {
                revengerPartiesCleanUp = false;
                DisbandAllRevengeParties();
            }

            for (int i = 0; i < revengeData.Count; i++) //Do not remove revengeData elsewhere (only should add in other threads or events)
            {
                if (revengeData[i].state == PeasantRevengeData.quest_state.ready)
                {
                    if (revengeData[i].startTime.IsPast)
                    {
                        revengeData[i].Begin();
                        if (_cfg.values.enableRevengerMobileParty)
                        {
                            if (revengeData[i].party != null && revengeData[i].party.MobileParty != null)
                            {
                                revengeData[i].xParty = CreateNotableParty(revengeData[i]);
                                revengeData[i].xParty.Ai.SetMoveEscortParty(revengeData[i].party.MobileParty);
                            }
                            else
                            {
                                revengeData[i].Stop();
                            }
                        }
                    }
                }

                if (revengeData[i].state == PeasantRevengeData.quest_state.begin)
                {
                    if (revengeData[i].executioner != null)
                    {
                        if (revengeData[i].xParty != null && revengeData[i].targetHero.HeroObject.PartyBelongedTo != null)
                        {
                            if (Hero.MainHero.PartyBelongedTo != null && revengeData[i].party != null && revengeData[i].party == Hero.MainHero.PartyBelongedTo.Party)
                            {
                                revengeData[i].Start();
                                if (revengeData[i].xParty.Position2D.Distance(revengeData[i].targetHero.HeroObject.PartyBelongedTo.Position2D) > _cfg.values.peasantRevengePartyTalkToLordDistance)
                                {
                                    revengeData[i].xParty.Ai.SetMoveGoToPoint(revengeData[i].targetHero.HeroObject.PartyBelongedTo.Position2D);
                                }
                            }
                            else
                            {
                                if (revengeData[i].xParty.Position2D.Distance(revengeData[i].targetHero.HeroObject.PartyBelongedTo.Position2D) > _cfg.values.peasantRevengePartyTalkToLordDistance)
                                {
                                    revengeData[i].xParty.Ai.SetMoveGoToPoint(revengeData[i].targetHero.HeroObject.PartyBelongedTo.Position2D);
                                }
                                else
                                {
                                    if (revengeData[i].criminal == Hero.MainHero.CharacterObject && revengeData[i].party != null && Hero.MainHero.PartyBelongedToAsPrisoner != null && revengeData[i].party == Hero.MainHero.PartyBelongedToAsPrisoner)
                                    {
                                        revengeData[i].Start();
                                        if (revengeData[i].Can_peasant_revenge_lord_start)
                                        {
                                            CampaignMapConversation.OpenConversation(
                                                new ConversationCharacterData(Hero.MainHero.CharacterObject, null, false, false, false, false, false, false),
                                                new ConversationCharacterData(revengeData[i].party.Owner.CharacterObject, revengeData[i].party, false, false, false, false, false, false));
                                            break;
                                        }
                                        else
                                        {
                                            revengeData[i].Stop();
                                        }
                                    }
                                    else
                                    {
                                        if (revengeData[i].targetHero != Hero.MainHero.CharacterObject)
                                        {
                                            if (RevengeAI(revengeData[i])) // if player dialog start after AI run
                                            {
                                                revengeData[i].Start();
                                                revengeData[i].nobleParty = revengeData[i].party;
                                                revengeData[i].targetHero = Hero.MainHero.CharacterObject;
                                            }
                                            else
                                            {
                                                revengeData[i].Stop();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!_cfg.values.enableRevengerMobileParty)
                            {
                                if ((Hero.MainHero.PartyBelongedTo == null ? false : revengeData[i].party == Hero.MainHero.PartyBelongedTo.Party) ||
                                    (revengeData[i].criminal == Hero.MainHero.CharacterObject))
                                {
                                    revengeData[i].Start();
                                }
                                else
                                {
                                    if (RevengeAI(revengeData[i])) // if player dialog start after AI run
                                    {
                                        revengeData[i].Start();
                                        revengeData[i].nobleParty = revengeData[i].party;
                                        revengeData[i].targetHero = Hero.MainHero.CharacterObject;
                                    }
                                    else
                                    {
                                        revengeData[i].Stop();
                                    }
                                }

                                if (revengeData[i].Can_peasant_revenge_peasant_start)
                                {
                                    CampaignMapConversation.OpenConversation(
                                       new ConversationCharacterData(Hero.MainHero.CharacterObject, null, false, false, false, false, false, false),
                                       new ConversationCharacterData(revengeData[i].executioner, null, false, false, false, false, false, false));
                                    break;
                                }
                                else if (revengeData[i].Can_peasant_revenge_lord_start)
                                {
                                    CampaignMapConversation.OpenConversation(
                                        new ConversationCharacterData(Hero.MainHero.CharacterObject, null, false, false, false, false, false, false),
                                        new ConversationCharacterData(revengeData[i].party.Owner.CharacterObject, revengeData[i].party, false, false, false, false, false, false));
                                    break;
                                }
                                else if (revengeData[i].Can_peasant_revenge_messenger_peasant_start)
                                {
                                    CampaignMapConversation.OpenConversation(
                                        new ConversationCharacterData(Hero.MainHero.CharacterObject, null, false, false, false, false, false, false),
                                        new ConversationCharacterData(revengeData[i].executioner, revengeData[i].nobleParty, false, false, false, false, false, false));
                                    break;
                                }
                                else
                                {
                                    revengeData[i].Stop();
                                }
                            }
                        }
                    }
                    else
                    {
                        revengeData[i].Stop();
                    }
                }
                else if (revengeData[i].state == PeasantRevengeData.quest_state.start)
                {
                    if (_cfg.values.enableRevengerMobileParty)
                    {
                        if (revengeData[i].xParty != null && revengeData[i].targetHero.HeroObject.PartyBelongedTo != null &&
                           (revengeData[i].Can_peasant_revenge_messenger_peasant_start || revengeData[i].Can_peasant_revenge_peasant_start))
                        {
                            Vec2 pposition = revengeData[i].targetHero.HeroObject.PartyBelongedTo.Position2D;
                            Vec2 rvec2 = new Vec2(_cfg.values.peasantRevengePartyWaitLordDistance >= 0.0f ? _cfg.values.peasantRevengePartyWaitLordDistance : 1.0f, 0f);
                            rvec2.RotateCCW(MBRandom.RandomFloatRanged(6.28f));
                            revengeData[i].xParty.Ai.SetMoveGoToPoint(pposition + rvec2);
                        }
                        else
                        {
                            revengeData[i].Stop();
                        }
                    }
                }

                if (revengeData[i].dueTime.IsPast)
                {
                    revengeData[i].Stop();
                }

                if (revengeData[i].state == PeasantRevengeData.quest_state.stop)
                {
                    if (revengeData[i].xParty != null)
                    {
                        if (revengeData[i].xParty.Position2D.Distance(revengeData[i].executioner.HeroObject.HomeSettlement.Position2D) < 2f)
                        {
                            if (!revengeData[i].executioner.HeroObject.HomeSettlement.IsUnderRaid)
                            {
                                DestroyPartyAction.ApplyForDisbanding(revengeData[i].xParty, revengeData[i].executioner.HeroObject.HomeSettlement); // will set clear flag in events
                            }
                        }
                        else
                        {
                            revengeData[i].xParty.Ai.SetMoveGoToSettlement(revengeData[i].executioner.HeroObject.HomeSettlement);
                        }
                    }
                    else
                    {
                        revengeData[i].Clear();
                    }
                }
            }
            lock (revengeData)
            {
                revengeData.RemoveAll((x) => ((x.state == PeasantRevengeData.quest_state.clear))); // remove here, because other events may interrupt this event 
            }

        }

        /// <summary>
        /// When AI caught the criminal non player. Peasant revenge evets should follow sequence: 
        /// raiding->hero captured the criminal->
        /// peasant demand hero's party to let revenge -> partyWillCareOfPeasantRevenge
        /// hero's party escort the peasant and demand village owner to aggree with revenge ->  (this option mus be with many conitions: hero's traits, kingdom policies, party hero's relations)
        /// hero's party escort the peasant and ask hero's clan leader for decision
        /// hero's party escort the peasant and ask criminal's clan leader for reparation
        /// </summary>
        /// <param name="revenge"></param>
        /// <returns></returns>
        private bool RevengeAI(PeasantRevengeData revenge)
        {
            Hero prisoner = revenge.criminal.HeroObject;
            Hero executioner = revenge.executioner.HeroObject;
            PartyBase party = revenge.party;
            Settlement settlement = revenge.village.Settlement;
            Hero saver = null;
            Hero ransomer = null; // pays unpaid ransom, if criminal is killed
            List<Hero> savers;
            string message = "";
            List<string> LogMessage = new List<string>();

            bool TheSameKingdom = party.Owner.Clan.Kingdom != null ? settlement.OwnerClan.Kingdom != null ? settlement.OwnerClan.Kingdom == party.Owner.Clan.Kingdom : false : false; // false for settlements or parties without kingdoms

            if (_cfg.values.otherKingdomClanCanCareOfPeasantRevenge == false) // do not allow allien party or settlement to interfere in revenge
            {
                if (!TheSameKingdom) // party or settlement is not in the same kingdom or is not part of any kingdom
                {
                    //Cannot to pay (Kingdom does not care)
                    LogMessage.Add("{=PRev0042}{PARTYOWNER.NAME} did not executed {PRISONER.NAME}, because different kingdom");
                    message = $"{party.Owner.Name} did not executed {prisoner.Name} because different kingdom.";
                    //ChangeRelationAction.ApplyRelationChangeBetweenHeroes(settlement.Owner, executioner, _cfg.values.relationChangeWhenCannotPayReparations, false); // Already Talewords implemented this                        
                    goto SkipToEnd;
                }
            }

            bool TheSameClan = settlement.OwnerClan == party.Owner.Clan;
            if (!_cfg.values.alwaysLetLiveTheCriminal)
            {
                revenge.accused_hero = getAllyPrisonerTheEscapeGoat(prisoner);

                if (revenge.accused_hero != null)
                {
                    log($"{prisoner.Name} blamed {revenge.accused_hero.Name} for looting the village.");
                    LogMessage.Add("{=PRev0069}{CRIMINAL.NAME} accused the {CRIMINALBLAMED.NAME} for looting the village.");
                    prisoner = revenge.accused_hero.HeroObject;
                }

                bool party_relatives_with_criminal_condition = (party.Owner.Children.Contains(prisoner) || prisoner.Children.Contains(party.Owner)) &&
                                                          CheckConditions(party.Owner, prisoner, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal);

                bool party_help_criminal_con = CheckConditions(party.Owner, executioner, _cfg.values.ai.lordWillAffordToHelpTheCriminalEnemy);
                bool party_friend_to_criminal_con = party.Owner.IsFriend(prisoner);
                bool party_overide_con = CheckConditions(party.Owner, executioner, _cfg.values.ai.partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet) || party.Owner.IsFriend(executioner);
                bool party_let_due_accusations = revenge.accused_hero != null ? !AIwillMakeNoDecisionDueConflict(party.Owner, revenge) : true;
                bool party_let_revenge_con = (!party_help_criminal_con && !party_friend_to_criminal_con && !party_relatives_with_criminal_condition && !party_let_due_accusations) || party_overide_con;

                if (party_let_revenge_con || _cfg.values.alwaysExecuteTheCriminal) //no conflict with party leader and peasant or override
                {
                    bool sellement_owner_relatives_with_criminal_condition = (settlement.Owner.Children.Contains(prisoner) || prisoner.Children.Contains(settlement.Owner)) &&
                                                   CheckConditions(settlement.Owner, prisoner, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal);
                    bool sellement_owner_help_criminal_con = CheckConditions(settlement.Owner, executioner, _cfg.values.ai.lordWillAffordToHelpTheCriminalEnemy);
                    bool sellement_owner_friend_to_criminal_con = settlement.Owner.IsFriend(prisoner);
                    bool sellement_owner_overide_con = CheckConditions(settlement.Owner, executioner, _cfg.values.ai.settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet);
                    bool sellement_owner_let_due_accusations = revenge.accused_hero != null ? !AIwillMakeNoDecisionDueConflict(settlement.Owner, revenge) : true;
                    bool sellement_owner_let_revenge_con = (!sellement_owner_help_criminal_con && !sellement_owner_friend_to_criminal_con && !sellement_owner_relatives_with_criminal_condition && !sellement_owner_let_due_accusations) || sellement_owner_overide_con;

                    if (sellement_owner_let_revenge_con || _cfg.values.alwaysExecuteTheCriminal) //no conflict with settlement leader and peasant or override
                    {
                        if (prisoner.Gold < revenge.reparation || sellement_owner_overide_con || party_overide_con)
                        {
                            //Cannot save criminal because hero clan deals with peasant revenge first 
                            //Must return here if player refuses to deal with criminal!!!
                            if (settlement.OwnerClan == Hero.MainHero.Clan && _cfg.values.alwwaysReportPeasantRevengeToClanLeader ||
                               (prisoner.Clan == Hero.MainHero.Clan)) //if prisoner is player's companion return to player too (not the same rules as AI) // harcore mode do not allow player to save companion - the same rules like AI
                            {
                                return true;
                            }

                            savers = GetHeroSuportersWhoCouldSaveVictim(prisoner, revenge.reparation);

                            if (!savers.IsEmpty())
                            {
                                saver = savers.Where((x) => x.IsHumanPlayerCharacter).IsEmpty() ? savers.GetRandomElementInefficiently() : savers.Where((x) => x.IsHumanPlayerCharacter).First();
                            }

                            if (savers.IsEmpty() || _cfg.values.alwaysExecuteTheCriminal)
                            {
                                #region Unpaid ransom
                                //AI get unpaid RANSOM
                                float ransomValue = (float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(prisoner.CharacterObject, null);
                                List<Hero> ransomers = GetHeroSuportersWhoCouldPayUnpaidRansom(prisoner, (int)ransomValue); // list who will buy dead body
                                List<Hero> own_clan_ransomers = GetHeroSuportersWhoCouldPayUnpaidRansom(party.Owner, (int)ransomValue); // interesting feature: if could get money from kingdom clan?

                                if (ransomers.IsEmpty())
                                {
                                    ransomers.AddRange(own_clan_ransomers);
                                }

                                string ransomstring = "";
                                bool ransom_of_prisoner_is_paid = false;
                                if (!ransomers.IsEmpty())
                                {
                                    ransomer = ransomers.GetRandomElementInefficiently();

                                    if (!ransomer.IsHumanPlayerCharacter)
                                    {
                                        if (WillLordDemandSupport(party.Owner))
                                        {
                                            if (WillLordSupportHeroClaim(ransomer, party.Owner))
                                            {
                                                ransom_of_prisoner_is_paid = true;
                                                GiveGoldAction.ApplyBetweenCharacters(ransomer, party.Owner, (int)ransomValue, true);
                                                ransomstring = $" {ransomer.Name} paid to {party.Owner.Name} compensation {ransomValue}. {ransomer.Name} gold is now {ransomer.Gold}.";
                                                saver = ransomer;
                                                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, ransomer, _cfg.values.relationChangeAfterLordPartyGotPaid, false);
                                                revenge.quest_Results.Add(PeasantRevengeData.quest_result.ransom_paid_to_party);
                                            }
                                            else
                                            {
                                                ransomstring = $" {ransomer.Name} did not paid {party.Owner.Name} compensation {ransomValue}. {ransomer.Name} gold is now {ransomer.Gold}.";
                                                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, ransomer, _cfg.values.relationChangeAfterLordPartyGotNoReward, false);
                                                revenge.quest_Results.Add(PeasantRevengeData.quest_result.ransom_not_paid_to_party);
                                                //does not have money for ransom
                                            }
                                        }
                                        else
                                        {
                                            //increase generosity? will lord accept ransom if offered ? (not done case)
                                        }
                                    }
                                    else
                                    {

                                        log($"Player was selected as ransomer of {prisoner.Name}. Payment is not implemented.");
                                    }
                                }
                                else
                                {//have no ransomer, but execution is go,means that party clan leader allow execution to stisfy peasant demands
                                    if (party.Owner != party.Owner.Clan.Leader)
                                    {
                                        if (WillLordDemandSupport(party.Owner))
                                        {
                                            ransomstring = $" {party.Owner.Clan.Leader.Name} does not have ransom money for {party.Owner.Name}'s asked ransom";
                                            //ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, party.Owner.Clan.Leader, _cfg.values.relationChangeAfterLordPartyGotNoReward, false);
                                        }
                                    }
                                }
                                #endregion

                                if (saver != null)
                                {
                                    LogMessage.Add("{=PRev0045}{SAVER.NAME} paid {REPARATION}{GOLD_ICON} for {PRISONER.NAME}'s head to {EXECUTIONER.NAME}.");
                                }

                                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, executioner, _cfg.values.relationChangeWhenLordExecutedTheCriminal, false);

                                if (_cfg.values.allowPeasantToKillLord)
                                {
                                    message = $"{party.Owner.Name} captured and {executioner.Name} executed {prisoner.Name} because lack {revenge.reparation - prisoner.Gold} gold. Reparation {revenge.reparation}." + ransomstring;
                                    KillCharacterAction.ApplyByExecution(prisoner, executioner, true, true);

                                }
                                else
                                {
                                    message = $"{party.Owner.Name} captured and executed {prisoner.Name} because lack {revenge.reparation - prisoner.Gold} gold. Reparation {revenge.reparation}." + ransomstring;
                                    KillCharacterAction.ApplyByExecution(prisoner, party.Owner, true, true);
                                }

                                revenge.quest_Results.Add(PeasantRevengeData.quest_result.criminal_killed);

                                if (ransom_of_prisoner_is_paid == false)
                                {
                                    message += AIDealWithLordRemains(revenge, party.Owner, prisoner);
                                }
                                #region killing criminal too
                                if (revenge.accused_hero != null)
                                {
                                    if (CheckConditions(party.Owner, revenge.criminal.HeroObject, _cfg.values.ai.lordWillKillBothAccusedHeroAndCriminalLord))
                                    {
                                        if (_cfg.values.allowPeasantToKillLord)
                                        {
                                            message = $"{executioner.Name} executed {revenge.criminal.Name} too.";
                                            KillCharacterAction.ApplyByExecution(revenge.criminal.HeroObject, executioner, true, true);
                                        }
                                        else
                                        {
                                            message = $"{party.Owner.Name} executed {revenge.criminal.Name} too.";
                                            KillCharacterAction.ApplyByExecution(revenge.criminal.HeroObject, party.Owner, true, true);
                                        }
                                        message += AIDealWithLordRemains(revenge, party.Owner, revenge.criminal.HeroObject);
                                        revenge.quest_Results.Add(PeasantRevengeData.quest_result.accused_hero_killed);
                                    }
                                    else
                                    {
                                        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, revenge.criminal.HeroObject,
                                            _cfg.values.relationChangeLordAndCriminalWhenLordExecutedTheAccusedCriminal, false);
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                //Must return here if player refuses tu deal with criminal!!!
                                if (saver.IsHumanPlayerCharacter)
                                {
                                    return true;
                                }
                                else
                                {
                                    if (hero_trait_list_condition(saver, _cfg.values.lordNotExecuteMessengerTrait))
                                    {//not Kill
                                        GiveGoldAction.ApplyBetweenCharacters(saver, executioner, (int)revenge.reparation, true);
                                        LogMessage.Add("{=PRev0040}{PARTYOWNER.NAME} did not executed {PRISONER.NAME}, because {SAVER.NAME} paid {REPARATION}{GOLD_ICON}.");
                                        message = $"{party.Owner.Name} did not executed {prisoner.Name}, because {saver.Name} paid {revenge.reparation} gold. Prisoner gold {prisoner.Gold}";
                                        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(saver, prisoner, _cfg.values.relationLordAndCriminalChangeWhenLordSavedTheCriminal, false); // because saver have expenses
                                        revenge.quest_Results.Add(PeasantRevengeData.quest_result.clan_paid);
                                    }
                                    else
                                    {
                                        if (_cfg.values.allowLordToKillMessenger)
                                        {
                                            LogMessage.Add("{=PRev0043}{PARTYOWNER.NAME} did not executed {PRISONER.NAME}, because {SAVER.NAME} executed peasant notable {EXECUTIONER.NAME}");
                                            message = $"{party.Owner.Name} did not executed {prisoner.Name}, because {saver.Name} executed peasant messenger {executioner.Name}. Saver gold {saver.Gold}. Prisoner gold {prisoner.Gold}.";
                                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, saver, _cfg.values.relationChangeWhenLordKilledMessenger, false);
                                            KillCharacterAction.ApplyByExecution(executioner, saver, false, false);
                                        }
                                        else
                                        {
                                            message = $"{party.Owner.Name} did not executed {prisoner.Name}, and {saver.Name} refused to pay to {executioner.Name}. Saver gold {saver.Gold}. Prisoner gold {prisoner.Gold}.";
                                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(executioner, saver, _cfg.values.relationChangeWhenCannotPayReparations, false);
                                        }
                                        revenge.quest_Results.Add(PeasantRevengeData.quest_result.messenger_killed);
                                    }
                                }
                            }
                        }
                        else
                        {//Have money, so maybe no kill?

                            if (settlement.OwnerClan == Hero.MainHero.Clan && _cfg.values.alwwaysReportPeasantRevengeToClanLeader || (party.Owner.Clan == Hero.MainHero.Clan)) // persuede party to always care of peasants ?
                            {
                                return true;
                            }
                            else// ai agreed to pay (here and  player companions as prisoners)
                            {
                                GiveGoldAction.ApplyBetweenCharacters(prisoner, executioner, (int)revenge.reparation, true);
                                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, executioner, _cfg.values.relationChangeAfterReparationsReceived, false);

                                LogMessage.Add("{=PRev0041}{PARTYOWNER.NAME} did not executed {PRISONER.NAME}, because {PRISONER.NAME} paid {REPARATION}{GOLD_ICON}.");
                                message = $"{party.Owner.Name} did not executed {prisoner.Name} because paid reparation of {revenge.reparation} gold. Savings left {prisoner.Gold}";
                                revenge.quest_Results.Add(PeasantRevengeData.quest_result.criminal_paid);
                            }
                        }
                    }
                    else
                    {
                        if (settlement.OwnerClan != Hero.MainHero.Clan)
                        {
                            string condition = "";

                            if (sellement_owner_friend_to_criminal_con) condition += " friend";
                            if (sellement_owner_help_criminal_con) condition += " lordWillAffordToHelpTheCriminal";
                            if (sellement_owner_relatives_with_criminal_condition) condition += " lordIfRelativesWillHelpTheCriminal";
                            if (!sellement_owner_overide_con) condition += " settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet";
                            if (!sellement_owner_let_due_accusations) condition += " sellement_owner_let_due_accusations";
                            message = $"Settlement owner {settlement.Owner.Name} refused to support {executioner.Name}'s revenge against {prisoner.Name}. ({condition})";
                            revenge.quest_Results.Add(PeasantRevengeData.quest_result.village_denied);
                            if (sellement_owner_friend_to_criminal_con)
                            {
                                LogMessage.Add("{=PRev0047}{SETTLEMENTOWNER.NAME} did not executed {PRISONER.NAME}, because friends.");
                            }
                            else if (sellement_owner_help_criminal_con)
                            {
                                LogMessage.Add("{=PRev0056}{SETTLEMENTOWNER.NAME} did not executed {PRISONER.NAME}, because have good relations.");
                            }
                            else if (sellement_owner_relatives_with_criminal_condition)
                            {
                                LogMessage.Add("{=PRev0057}{SETTLEMENTOWNER.NAME} did not executed {PRISONER.NAME}, because are relatives.");
                            }
                            else if (!sellement_owner_let_due_accusations)
                            {
                                LogMessage.Add("{=PRev0104}{SETTLEMENTOWNER.NAME} did not executed {PRISONER.NAME}, because of conflicting accusations.");
                            }
                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(settlement.Owner, executioner, _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge, false);
                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(settlement.Owner, prisoner, -1 * _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge, false);
                        }

                        if (settlement.OwnerClan == Hero.MainHero.Clan && party.Owner.Clan == Hero.MainHero.Clan && _cfg.values.alwwaysReportPeasantRevengeToClanLeader)
                        {
                            //ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, executioner, _cfg.values.relationChangeWhenLordRefusedToPayReparations, true);
                            return true; // start discuss peasant revenge with party lord who refused to kill criminal
                        }
                        else
                        {
                            //ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, executioner, _cfg.values.relationChangeWhenLordRefusedToPayReparations, false);
                        }
                    }
                }
                else
                {
                    string condition = "";
                    if (party_friend_to_criminal_con) condition += " friend";
                    if (party_help_criminal_con) condition += " lordWillAffordToHelpTheCriminal";
                    if (party_relatives_with_criminal_condition) condition += " lordIfRelativesWillHelpTheCriminal";
                    if (!party_overide_con) condition += " settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet";
                    if (!party_let_due_accusations) condition += " party_let_due_accusations";
                    message = $"Party {party.Owner.Name} refused to support {executioner.Name}'s revenge against {prisoner.Name}. ({condition})";
                    revenge.quest_Results.Add(PeasantRevengeData.quest_result.party_denied);
                    if (party_friend_to_criminal_con)
                    {
                        LogMessage.Add("{=PRev0044}{PARTYOWNER.NAME} did not executed {PRISONER.NAME}, because friends.");
                    }
                    else if (party_help_criminal_con)
                    {
                        LogMessage.Add("{=PRev0058}{PARTYOWNER.NAME} did not executed {PRISONER.NAME}, because have good relations.");
                    }
                    else if (party_relatives_with_criminal_condition)
                    {
                        LogMessage.Add("{=PRev0059}{PARTYOWNER.NAME} did not executed {PRISONER.NAME}, because are relatives.");
                    }
                    else if (!party_let_due_accusations)
                    {
                        LogMessage.Add("{=PRev0103}{PARTYOWNER.NAME} did not executed {PRISONER.NAME}, because of conflicting accusations.");
                    }
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, executioner, _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge, party.Owner.Clan == Hero.MainHero.Clan && _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge != 0);
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, prisoner, -1 * _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge, party.Owner.Clan == Hero.MainHero.Clan && _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge != 0);

                    if (settlement.OwnerClan == Hero.MainHero.Clan && party.Owner.Clan == Hero.MainHero.Clan && _cfg.values.alwwaysReportPeasantRevengeToClanLeader)
                    {
                        //ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, executioner, _cfg.values.relationChangeWhenLordRefusedToPayReparations, true);
                        return true; // start discuss peasant revenge with party lord who refused to kill criminal
                    }
                    else
                    {

                        //ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.Owner, executioner, _cfg.values.relationChangeWhenLordRefusedToPayReparations, false);
                    }
                }
            }
            else
            {
                //always let live
            }

        SkipToEnd:
            #region Log messages

            if (!string.IsNullOrEmpty(message))
            {
                log(message);

                if (!LogMessage.IsEmpty())
                {
                    if (_cfg.values.showPeasantRevengeLogMessages ||
                       (_cfg.values.showPeasantRevengeLogMessagesForKingdom && (party.Owner.Clan.Kingdom == Hero.MainHero.Clan.Kingdom || prisoner.Clan.Kingdom == Hero.MainHero.Clan.Kingdom))
                       )
                    {
                        foreach (string logMessage in LogMessage)
                        {
                            TaleWorlds.Localization.TextObject textObject = new TaleWorlds.Localization.TextObject(logMessage, null);
                            StringHelpers.SetCharacterProperties("PRISONER", prisoner.CharacterObject, textObject, false);
                            if (revenge.accused_hero != null)
                            {
                                StringHelpers.SetCharacterProperties("CRIMINALBLAMED", revenge.accused_hero, textObject, false);
                                StringHelpers.SetCharacterProperties("CRIMINAL", revenge.criminal, textObject, false);
                            }
                            StringHelpers.SetCharacterProperties("PARTYOWNER", party.Owner.CharacterObject, textObject, false);
                            StringHelpers.SetCharacterProperties("EXECUTIONER", executioner.CharacterObject, textObject, false);
                            StringHelpers.SetCharacterProperties("SETTLEMENTOWNER", settlement.Owner.CharacterObject, textObject, false);
                            textObject.SetTextVariable("REPARATION", (float)revenge.reparation);

                            if (saver != null)
                            {
                                StringHelpers.SetCharacterProperties("SAVER", saver.CharacterObject, textObject, false);
                            }

                            // InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
                            if (prisoner.Clan == Hero.MainHero.Clan || party.Owner.Clan == Hero.MainHero.Clan || executioner.HomeSettlement.OwnerClan == Hero.MainHero.Clan)
                            {
                                InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Color.ConvertStringToColor(_cfg.values.logColorForClan)));
                            }
                            else if (Hero.MainHero.Clan.Kingdom != null &&
                                (prisoner.Clan.Kingdom == Hero.MainHero.Clan.Kingdom || party.Owner.Clan.Kingdom == Hero.MainHero.Clan.Kingdom ||
                                executioner.HomeSettlement.OwnerClan.Kingdom == Hero.MainHero.Clan.Kingdom))
                            {
                                InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Color.ConvertStringToColor(_cfg.values.logColorForKingdom)));
                            }
                            else
                            {
                                InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Color.ConvertStringToColor(_cfg.values.logColorForOtherFactions)));
                            }
                        }
                    }
                }
            }
            #endregion
            return false;
        }

        private string AIDealWithLordRemains(PeasantRevengeData revenge, Hero owner, Hero victim)
        {
            string message = "";
            if (lordWillAbandonTheVictimRemains(owner, victim))
            {
                OnLordRemainsAbandoned(owner);
            }
            else
            {
                float ransomValue = (float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(victim.CharacterObject, null);
                List<Hero> ransomers = GetHeroSuportersWhoCouldPayUnpaidRansom(victim, (int)ransomValue);
                Hero ransomer;

                if (!ransomers.IsEmpty())
                {
                    ransomer = ransomers.GetRandomElementInefficiently();
                    if (ransomer.IsHumanPlayerCharacter)
                    {
                        AcceptRansomRemainsOffer((int)ransomValue, owner, ransomer); //because player related hero death scenes are enabled 
                    }
                    else
                    {
                        if (lordWillDeclineRansomTheVictimRemains(owner, victim))
                        {
                            AddKilledLordsCorpses(revenge);
                            OnRansomRemainsOfferDeclined(owner);
                            message = $" {owner.Name} declined to ransom {victim.Name}'s remains.";
                        }
                        else
                        {
                            AcceptRansomRemainsOffer((int)ransomValue, owner, ransomer);
                        }
                    }
                }
            }

            return message;
        }

        private bool lordWillDeclineRansomTheVictimRemains(Hero owner, Hero victim)
        {
            return CheckConditions(owner, victim, _cfg.values.ai.lordWillDeclineRansomTheVictimRemains);
        }

        private bool lordWillAbandonTheVictimRemains(Hero owner, Hero victim)
        {
            return CheckConditions(owner, victim, _cfg.values.ai.lordWillAbandonTheVictimRemains);
        }

        private MobileParty CreateNotableParty(PeasantRevengeData revenge)
        {

            int size = (int)revenge.executioner.HeroObject.HomeSettlement.Village.Hearth >= _cfg.values.peasantRevengeMaxPartySize - 1 ? _cfg.values.peasantRevengeMaxPartySize - 1 : (int)revenge.executioner.HeroObject.HomeSettlement.Village.Hearth;
            MobileParty mobileParty = MobileParty.CreateParty($"{revengerPartyNameStart}{revenge.executioner.Name}".Replace(' ', '_'), null, null);
            CharacterObject villager = revenge.executioner.Culture.Villager;
            TroopRoster troopRoster = new TroopRoster(mobileParty.Party);
            TextObject textObject = new TextObject("{=PRev0085}Revenger", null);
            troopRoster.AddToCounts(revenge.executioner, 1, true, 0, 0, true, -1);
            troopRoster.AddToCounts(villager, size, false, 0, 0, true, -1);
            mobileParty.InitializeMobilePartyAtPosition(troopRoster, new TroopRoster(mobileParty.Party), revenge.executioner.HeroObject.HomeSettlement.Position2D);
            mobileParty.InitializePartyTrade(200);
            mobileParty.SetCustomName(textObject);
            mobileParty.SetCustomHomeSettlement(revenge.executioner.HeroObject.HomeSettlement);
            mobileParty.SetPartyUsedByQuest(true);
            mobileParty.ShouldJoinPlayerBattles = false;
            mobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"), size);
            mobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("butter"), size);
            mobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("cheese"), size);
            //mobileParty.IgnoreForHours(_cfg.values.peasantRevengeTimeoutInDays*24f*10f); //if not ignored, ai can kill them and notable will respawn in the village
            mobileParty.Ai.SetDoNotMakeNewDecisions(true);
            mobileParty.Party.SetVisualAsDirty();
            mobileParty.Aggressiveness = 0f;
            return mobileParty;
        }

        private CharacterObject GetRevengeNotable(Settlement settlement)
        {
            int k = -1;

            if(settlement.Notables.Count <= 1) // do not allow to take all notables from village, because will get crash at line 208 in  TaleWorlds.CampaignSystem.GameComponents.GetRelationIncreaseFactor() after any other criminal raid party is defeated nearby
            {
                return null;
            }

            if (_cfg.values.peasantRevengerIsRandom)
            {
                k = MBRandom.RandomInt(0, settlement.Notables.Count - 1);
            }
            else
            {
                var valid = settlement.Notables.Where((x) => !hero_trait_list_condition(x, _cfg.values.peasantRevengerExcludeTrait) && x.Issue == null);
                if (valid.IsEmpty())
                {
                    log($"Village {settlement.Name} notables cannot demand revenge.");
                    return null;
                }
                else
                {
                    k = settlement.Notables.IndexOf(valid.ElementAt(MBRandom.RandomInt(0, valid.Count() - 1)));
                }
            }

            if (k >= 0 && k < settlement.Notables.Count)
            {
                return settlement.Notables.ElementAt(k).CharacterObject;
            }
            else
            {
                return null;
            }
        }

        private bool hero_trait_list_condition(Hero hero, string conditions, params Hero[] target)
        {
            if (string.IsNullOrEmpty(conditions)) return true;

            string[] equation;

            conditions.Replace(";", "&"); // compatibility

            equation = conditions.Split('|');

            bool result = false;

            foreach (string equationItem in equation)
            {
                bool ANDresult = false;
                if (equationItem.Contains("&"))
                {
                    ANDresult = true;
                    string[] equationAND = equationItem.Split('&');
                    for (int i = 0; i < equationAND.Length; i++)
                    {
                        string[] a = equationAND[i].Split(' ');
                        if (a.Length == 3)
                        {
                            if (a[0] == "Relations")
                            {
                                for (int k = 0; k < target.Length; k++)
                                {
                                    ANDresult = ANDresult && hero_relation_on_condition(hero, target[k], a[1], a[2]);
                                }
                            }
                            else
                            {
                                ANDresult = ANDresult && hero_trait_on_condition(hero, a[0], a[1], a[2]);
                            }
                        }
                        else
                        {
                            log("Error in equation: " + equationAND.ToString() + ". Now will be using default cfg. Please fix or Delete cfg file.");
                            ResetConfiguration();
                            break;
                        }
                    }
                }
                else
                {
                    string[] a = equationItem.Split(' ');
                    if (a.Length == 3)
                    {
                        if (a[0] == "Relations")
                        {
                            for (int k = 0; k < target.Length; k++)
                            {
                                ANDresult = hero_relation_on_condition(hero, target[k], a[1], a[2]);
                            }
                        }
                        else
                        {
                            ANDresult = hero_trait_on_condition(hero, a[0], a[1], a[2]);
                        }
                    }
                    else
                    {
                        log("Error in equation: " + equationItem.ToString() + ". Now will be using default cfg. Please fix or Delete cfg file.");
                        ResetConfiguration();
                        break;
                    }
                }

                result = result || ANDresult;
            }

            return result;
        }

        private bool hero_relation_on_condition(Hero hero, Hero target, string operation, string weight)
        {
            if (hero == null || target == null) return false;

            int value = hero.GetRelation(target);

            bool result = operation == "==" ? value == int.Parse(weight) :
                          operation == ">=" ? value >= int.Parse(weight) :
                          operation == "<=" ? value <= int.Parse(weight) :
                          operation == ">" ? value > int.Parse(weight) :
                          operation == "<" ? value < int.Parse(weight) :
                          operation == "!=" ? value != int.Parse(weight) : false;
            return result;
        }

        private bool hero_trait_on_condition(Hero hero, string tag, string operation, string weight)
        {
            if (hero == null) return false;

            int value = GetHeroTraitValue(hero, tag);

            bool result = operation == "==" ? value == int.Parse(weight) :
                          operation == ">=" ? value >= int.Parse(weight) :
                          operation == "<=" ? value <= int.Parse(weight) :
                          operation == ">" ? value > int.Parse(weight) :
                          operation == "<" ? value < int.Parse(weight) :
                          operation == "!=" ? value != int.Parse(weight) : false;
            return result;
        }

        private static int GetHeroTraitValue(Hero hero, string tag)
        {
            CharacterTraits ht = hero.GetHeroTraits();
            PropertyInfo[] props = ht.GetType().GetProperties();
            var prop = props.Where((x) => x.Name == tag).FirstOrDefault();
            if (prop == null)
            {
                return 0;
            }

            int value = (int)prop.GetValue((object)ht);

            return value;
        }

        private static void SetHeroTraitValue(Hero hero, string tag, int value)
        {
            hero.SetTraitLevel(TraitObject.All.Where((x) => x.StringId.ToString() == tag).First(), value);
        }
        #region Configuration 
        private void ResetConfiguration()
        {
            _cfg = new PeasantRevengeModCfg();
            _cfg.values.ai = new PeasantRevengeConfiguration.AIfilters();
            _cfg.values.ai.Default();
        }
        private void SetEnableRevengerMobileParty(bool value)
        {
            _cfg.values.enableRevengerMobileParty = value;
            _cfg.Save(_cfg.values.file_name, _cfg.values);
        }

        private void SetEnableHelpNeutralVillage(bool value)
        {
            _cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu = value;
            _cfg.Save(_cfg.values.file_name, _cfg.values);
        }

        private void LoadConfiguration(CampaignGameStarter campaignGameStarter)
        {
            int defaultVersion = (new PeasantRevengeConfiguration()).CfgVersion;

            if (File.Exists(_cfg.values.file_name))
            {
                _cfg.Load(_cfg.values.file_name, typeof(PeasantRevengeConfiguration));

                if (_cfg.values.ai == null)
                {
                    _cfg.values.ai = new PeasantRevengeConfiguration.AIfilters();
                    _cfg.values.ai.Default();
                }
                else
                {
                    // configuration patch for new added configuration variables

                    if (_cfg.values.CfgVersion < 14)
                    {
                        _cfg.values.ai.default_criminalWillBlameOtherLordForTheCrime();
                        _cfg.values.ai.default_lordWillKillBothAccusedHeroAndCriminalLord();
                    }

                    if (_cfg.values.CfgVersion < 15)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenRansomRemainsDeclined();
                        _cfg.values.ai.default_lordTraitChangeWhenRansomRemainsAccepted();
                        _cfg.values.ai.default_lordTraitChangeWhenRemainsOfLordAreAbandoned();
                        _cfg.values.ai.default_lordWillDeclineRansomTheVictimRemains();
                        _cfg.values.ai.default_lordWillAbandonTheVictimRemains();
                    }

                    if (_cfg.values.CfgVersion < 16)
                    {
                        _cfg.values.ai.default_lordWillNotKillBothAccusedHeroAndCriminalLordDueConflict();
                    }

                    if (_cfg.values.CfgVersion < 17)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest();
                    }
                    if (_cfg.values.CfgVersion < 18)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenLordPersuedeNotableNotToRevenge();
                        _cfg.values.ai.default_lordTraitChangeWhenLordPersuedeNotableToRevenge();
                    }
                    if (_cfg.values.CfgVersion < 19)
                    {
                        _cfg.values.ai.default_notableWillAcceptTheBribe();
                    }
                }
            }
            else
            {
                if (_cfg.values.ai == null)
                {
                    _cfg.values.ai = new PeasantRevengeConfiguration.AIfilters();
                    _cfg.values.ai.Default();
                }
            }

            _cfg.values = CheckModules(_cfg.values); // leave loaded cfg or change cfg only if needed !

            if (defaultVersion > _cfg.values.CfgVersion || !File.Exists(_cfg.values.file_name))
            {
                #region configuration patch

                if (_cfg.values.CfgVersion == 14)
                {
                    _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge =
                        _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge == -2 ? -1 : _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge; //reduced, because lords may lose recruitement village too fast
                }

                _cfg.values.CfgVersion = defaultVersion;
                #endregion

                bool can_save = false;

                try
                {
                    if (Directory.GetDirectories(_cfg.values.file_name) != null)
                    {
                        can_save = true;
                    }
                }
                catch
                {
                    //
                }
                finally
                {
                    if (!can_save)
                    {
                        _cfg.values.file_name = PeasantRevengeConfiguration.default_file_name();
                    }
                }

                _cfg.Save(_cfg.values.file_name, _cfg.values);
            }

            AddDialogs(campaignGameStarter);
            AddRaidingParties();
            Test();
        }

        private void OnGameLoadedEvent(CampaignGameStarter campaignGameStarter)
        {
            LoadConfiguration(campaignGameStarter);
            AddGameMenus(campaignGameStarter);
        }
        #endregion

        void Test()
        {
            int sum = 0;
            int total = 0;
            int sum_hearts = 0;
            float max_hearts = 0;
            float min_hearts = 100000;
            foreach (Settlement s in Settlement.All)
            {
                if (s.IsVillage)
                {
                    sum_hearts += (int)s.Village.Hearth;
                    min_hearts = s.Village.Hearth < min_hearts ? s.Village.Hearth : min_hearts;
                    max_hearts = s.Village.Hearth > max_hearts ? s.Village.Hearth : max_hearts;
                    total++;
                    var x = GetRevengeNotable(s.Village.Settlement);
                    if (x != null)
                    {
                        sum++;
                    }
                }
            }

            log($"Total vilages {total}. Can revenge :{sum}. Average hearts: {(sum_hearts / total)}. MinHearts {min_hearts}. MaxHearts{max_hearts}");

            List<PeasantRevengeConfiguration.RelationsPerTraits> criminalWillBlameOtherLordForTheCrime = _cfg.values.ai.criminalWillBlameOtherLordForTheCrime;

            foreach (Hero s in Hero.AllAliveHeroes)
            {
                if (s.IsLord)
                {
                    int victims = 0;
                    int both = 0;
                    foreach (Hero h in Hero.AllAliveHeroes)
                    {
                        if (s.IsLord && s.Id.ToString() != h.Id.ToString())
                        {
                            if (CheckConditions(s, h, criminalWillBlameOtherLordForTheCrime))
                            {
                                victims++;
                            }
                            if (CheckConditions(s, h, _cfg.values.ai.lordWillKillBothAccusedHeroAndCriminalLord))
                            {
                                both++;
                            }
                        }
                    }
                    log($" {s.Name}  {s.Gold} {s.Clan?.Name} {(victims > 0 ? "blame: " + victims.ToString() : "")} {(both > 0 ? "both: " + both.ToString() : "")}");
                }
            }
        }

        void AddRaidingParties()
        {
            //try to add parties who where missed in VillageBeingRaided (after game load usually)
            foreach (Hero criminal in Hero.AllAliveHeroes)
            {
                if (criminal.IsLord)
                {
                    var settlements = Settlement.All.Where(x => (
                    (x.LastAttackerParty != null ?
                    (x.LastAttackerParty.Owner == criminal && x.IsUnderRaid && !criminal.IsPrisoner) : false) && x.IsVillage));

                    if (settlements != null && !settlements.IsEmpty())
                    {
                        //#if DEBUG
                        //                        if (settlements.Count() > 0)
                        //                        {
                        //                            log($"[AddRaidingParties] {criminal.Name} added and has {settlements.Count()} settlement records ({settlements.Last().Village}; {CampaignTime.DaysFromNow(_cfg.values.peasantRevengeTimeoutInDays)})");
                        //                        }
                        //#endif
                        revengeData.Add(
                            new PeasantRevengeData
                            {
                                village = settlements.Last().Village,
                                criminal = criminal.CharacterObject,
                                dueTime = CampaignTime.DaysFromNow(_cfg.values.peasantRevengeTimeoutInDays)
                            });
                    }
                }
            }
        }

        void DisbandAllRevengeParties()
        {
            IEnumerable<MobileParty> parties = MobileParty.AllPartiesWithoutPartyComponent.Where((x) => x.IsCurrentlyUsedByAQuest && x.StringId.StartsWith(revengerPartyNameStart) && x.IsActive);
            for (int i = 0; i < parties.Count(); i++)
            {
                TroopRoster troopsLordParty = parties.ElementAt(i).MemberRoster;
                for (int j = 0; j < troopsLordParty.Count; j++)
                {
                    CharacterObject troop = troopsLordParty.GetCharacterAtIndex(j);
                    if (troop.IsHero)
                    {
                        DestroyPartyAction.ApplyForDisbanding(parties.ElementAt(i), troop.HeroObject.HomeSettlement);
                        break;
                    }
                }
            }
        }

        private bool CanAffordToSpendMoney(Hero hero, int goldNeeded, List<PeasantRevengeConfiguration.MoneyPerTraits> traits)
        {
            if (hero.Gold == 0 || hero.Gold < goldNeeded) return false;

            int percent = 100 * goldNeeded / hero.Gold;

            foreach (PeasantRevengeConfiguration.MoneyPerTraits mpt in traits)
            {
                if (mpt.percent >= percent)
                {
                    if (hero_trait_list_condition(hero, mpt.traits))
                    {
                        return true;
                    }
                }
            }
            return true;
        }

        private bool CheckConditions(Hero hero, Hero target, List<PeasantRevengeConfiguration.RelationsPerTraits> traits)
        {
            if (traits.IsEmpty()) return true;

            foreach (PeasantRevengeConfiguration.RelationsPerTraits rpt in traits)
            {
                if (hero_trait_list_condition(hero, rpt.relations, target))
                {
                    if (hero_trait_list_condition(hero, rpt.traits, target))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private List<Hero> GetHeroSuportersWhoCouldSaveVictim(Hero victim, int goldNeeded)
        {
            List<Hero> list = new List<Hero>();

            if (victim.Clan != null)
            {
                if (!string.IsNullOrEmpty(_cfg.values.log_file_name))
                {
                    log($"Saver Check in Clan:");
                    foreach (Hero x in victim.Clan.Heroes)
                    {
                        if (x.IsAlive)
                        {
                            bool age_con = x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge;
                            bool money_con = CanAffordToSpendMoney(x, goldNeeded, _cfg.values.ai.lordWillAffordPartOfHisSavingsToPayForFavor);
                            bool relations_min_con = x.GetRelation(victim) >= _cfg.values.criminalHeroFromClanSuporterMinimumRelation;
                            bool relations_set_con = CheckConditions(x, victim, _cfg.values.ai.lordWillAffordToHelpTheCriminalAlly);
                            bool child_cond = ((x.Children.Contains(victim) || (victim.Children.Contains(x) && x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge)) &&
                                               CheckConditions(x, victim, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal));
                            bool friend_con = (x.IsFriend(victim) && CheckConditions(x, victim, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal));
                            bool not_enemy_con = !x.IsEnemy(victim);
                            bool have_gold = x.Gold >= goldNeeded;
                            if (age_con)
                            {
                                log($"Gold {Convert.ToInt32(have_gold)}\tGoldAvailable {Convert.ToInt32(money_con)}\tRelationsMin {Convert.ToInt32(relations_min_con)}\tRelationsCanHelp {Convert.ToInt32(relations_set_con)}\tRelative {Convert.ToInt32(child_cond)}\tFriend {Convert.ToInt32(friend_con)}\tNotEnemy {Convert.ToInt32(not_enemy_con)}\t{x.Name}");
                            }
                        }
                    }
                }

                list.AddRange(victim.Clan.Heroes.Where((x) => (
                  //x != victim && // can save self         
                  x.IsAlive &&
                  x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge &&
                  CanAffordToSpendMoney(x, goldNeeded, _cfg.values.ai.lordWillAffordPartOfHisSavingsToPayForFavor) &&
                  !x.IsEnemy(victim) &&
                  x.GetRelation(victim) >= _cfg.values.criminalHeroFromClanSuporterMinimumRelation && //this will block all lesser relations
                 (
                   CheckConditions(x, victim, _cfg.values.ai.lordWillAffordToHelpTheCriminalAlly) || // if not relative, friend or clan leader                 
                   ((x.Children.Contains(victim) || victim.Children.Contains(x) && CheckConditions(x, victim, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal)) ||
                   (x.IsFriend(victim) && CheckConditions(x, victim, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal))
                  )))).ToList());

                if (_cfg.values.alowKingdomClanToSaveTheCriminal)
                {
                    if (victim.Clan.Kingdom != null && list.IsEmpty())
                    {
                        if (!string.IsNullOrEmpty(_cfg.values.log_file_name))
                        {
                            log($"Saver Check in Kingdom:");
                            foreach (Hero x in victim.Clan.Kingdom.Heroes)
                            {
                                if (x.IsAlive && x.Clan != victim.Clan && x != victim)
                                {
                                    bool age_con = x.Age >= _cfg.values.criminalHeroFromKingdomSuporterMinimumAge;
                                    bool money_con = CanAffordToSpendMoney(x, goldNeeded, _cfg.values.ai.lordWillAffordPartOfHisSavingsToPayForFavor);
                                    bool relations_min_con = x.GetRelation(victim) >= _cfg.values.criminalHeroFromKingdomSuporterMinimumRelation;
                                    bool relations_set_con = CheckConditions(x, victim, _cfg.values.ai.lordWillAffordToHelpTheCriminalAlly);
                                    bool child_cond = ((x.Children.Contains(victim) || (victim.Children.Contains(x) && x.Age >= _cfg.values.criminalHeroFromKingdomSuporterMinimumAge)) &&
                                                       CheckConditions(x, victim, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal));
                                    bool friend_con = (x.IsFriend(victim) && CheckConditions(x, victim, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal));
                                    bool not_enemy_con = !x.IsEnemy(victim);
                                    bool have_gold = x.Gold >= goldNeeded;
                                    if (age_con)
                                    {
                                        log($"Gold {Convert.ToInt32(have_gold)}\tGoldAvailable {Convert.ToInt32(money_con)}\tRelationsMin {Convert.ToInt32(relations_min_con)}\tRelationsCanHelp {Convert.ToInt32(relations_set_con)}\tRelative {Convert.ToInt32(child_cond)}\tFriend {Convert.ToInt32(friend_con)}\tNotEnemy {Convert.ToInt32(not_enemy_con)}\t{x.Name}");
                                    }
                                }
                            }
                        }

                        list.AddRange(victim.Clan.Kingdom.Heroes.Where((x) => (
                        x.Clan != victim.Clan &&
                        x.IsAlive &&
                        x.Age >= _cfg.values.criminalHeroFromKingdomSuporterMinimumAge &&
                        CanAffordToSpendMoney(x, goldNeeded, _cfg.values.ai.lordWillAffordPartOfHisSavingsToPayForFavor) &&
                        !x.IsEnemy(victim) &&
                        x.GetRelation(victim) >= _cfg.values.criminalHeroFromKingdomSuporterMinimumRelation &&
                        (
                         CheckConditions(x, victim, _cfg.values.ai.lordWillAffordToHelpTheCriminalAlly) ||
                         ((x.Children.Contains(victim) || (victim.Children.Contains(x) && x.Age >= _cfg.values.criminalHeroFromKingdomSuporterMinimumAge)) &&
                         CheckConditions(x, victim, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal)) ||
                         (x.IsFriend(victim) && CheckConditions(x, victim, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal))
                        ))).ToList());
                    }
                }
            }

            return list;
        }

        private List<Hero> GetHeroSuportersWhoCouldPayUnpaidRansom(Hero hero, int goldNeeded)
        {
            List<Hero> list = new List<Hero>();

            if (hero.Clan != null)
            {
                if (!string.IsNullOrEmpty(_cfg.values.log_file_name))
                {
                    log($"Ransom Check:");
                    foreach (Hero x in hero.Clan.Heroes)
                    {
                        if (x.IsAlive && x != hero)
                        {
                            bool age_con = x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge;
                            bool money_con = CanAffordToSpendMoney(x, goldNeeded, _cfg.values.ai.lordWillAffordPartOfHisSavingsToPayForFavor);
                            bool relations_min_con = x.GetRelation(hero) >= _cfg.values.criminalHeroFromClanSuporterMinimumRelation;
                            bool relations_set_con = CheckConditions(x, hero, _cfg.values.ai.lordWillAffordToHelpPayLostRansom);
                            bool child_cond = ((x.Children.Contains(hero) || (hero.Children.Contains(x) && x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge)) &&
                                               CheckConditions(x, hero, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal));
                            bool friend_con = (x.IsFriend(hero) && CheckConditions(x, hero, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal));
                            bool not_enemy_con = !x.IsEnemy(hero);
                            bool have_gold = x.Gold >= goldNeeded;
                            if (age_con)
                            {
                                log($"Gold {Convert.ToInt32(have_gold)}\tGoldAvailable {Convert.ToInt32(money_con)}\tRelationsMin {Convert.ToInt32(relations_min_con)}\tRelationsCanHelp {Convert.ToInt32(relations_set_con)}\tRelative {Convert.ToInt32(child_cond)}\tFriend {Convert.ToInt32(friend_con)}\tNotEnemy {Convert.ToInt32(not_enemy_con)}\t{x.Name}");
                            }
                        }
                    }
                }

                list.AddRange(hero.Clan.Heroes.Where((x) => (
                 x != hero && //victim was executed!                
                 x.IsAlive &&
                 x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge &&
                 CanAffordToSpendMoney(x, goldNeeded, _cfg.values.ai.lordWillAffordPartOfHisSavingsToPayForFavor) &&
                 !x.IsEnemy(hero) &&
                 x.GetRelation(hero) >= _cfg.values.criminalHeroFromClanSuporterMinimumRelation && //this will block all lesser relations
                (
                  CheckConditions(x, hero, _cfg.values.ai.lordWillAffordToHelpPayLostRansom) || // if not relative, friend or clan leader                 
                  ((x.Children.Contains(hero) || hero.Children.Contains(x) && CheckConditions(x, hero, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal)) ||
                  (x.IsFriend(hero) && CheckConditions(x, hero, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal))
                 )))).ToList());
            }

            return list;
        }

        private bool AIwillMakeNoDecisionDueConflict(Hero hero, PeasantRevengeData revenge)
        {
            bool traits_and_relations_with_criminal = CheckConditions(hero, revenge.criminal.HeroObject, _cfg.values.ai.lordWillNotKillBothAccusedHeroAndCriminalLordDueConflict);

            bool for_criminal = traits_and_relations_with_criminal;

            bool traits_and_relations_with_accused = CheckConditions(hero, revenge.accused_hero.HeroObject, _cfg.values.ai.lordWillNotKillBothAccusedHeroAndCriminalLordDueConflict);

            bool for_accused = traits_and_relations_with_accused;

            bool decision = for_accused && for_criminal;

            return decision;
        }

        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            #region Peasant revenge configuration via dialog
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_config_mod_start",
               "hero_main_options",
               "peasant_revenge_player_config_mod_options_set",
               "{=PRev0095}There is something I'd like to discuss.",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_player_config_mod_start_condition), null, 100, null);
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_player_config_mod_npc_options",
              "peasant_revenge_player_config_mod_options_set",
              "peasant_revenge_player_config_mod_options_set",
              "{=PRev0084}Yes, my {?MAINHERO.GENDER}Lady{?}Lord{\\?}.[rf:convo_thinking]", null, null, 200, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_config_mod_option_mp_dis",
               "peasant_revenge_player_config_mod_options_set",
               "peasant_revenge_player_config_mod_end_dis",
               "{=PRev0081}You should not immediately interrupt me with any your matter.",
                () => { return !_cfg.values.enableRevengerMobileParty; }, () => { SetEnableRevengerMobileParty(true); }, 100,
                new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_party_clickable_condition));
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_config_mod_option_mp_en",
               "peasant_revenge_player_config_mod_options_set",
               "peasant_revenge_player_config_mod_end_en",
               "{=PRev0082}You should immediately interrupt me with any your matter.",
                () => { return _cfg.values.enableRevengerMobileParty; }, () => { SetEnableRevengerMobileParty(false); }, 100,
                new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_party_clickable_condition));
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_player_config_mod_option_np_en",
              "peasant_revenge_player_config_mod_options_set",
              "peasant_revenge_player_config_mod_end_dis",
              "{=PRev0092}I'll defend villages against any looters.",
               () => { return !_cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu; }, () => { SetEnableHelpNeutralVillage(true); }, 100,
               new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_neutral_village_attack_clickable_condition));
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_player_config_mod_option_np_dis",
              "peasant_revenge_player_config_mod_options_set",
              "peasant_revenge_player_config_mod_end_en",
              "{=PRev0093}I will defend villages against my enemies only.",
               () => { return _cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu; }, () => { SetEnableHelpNeutralVillage(false); }, 100,
               new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_neutral_village_attack_clickable_condition));
            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_player_config_mod_option_exit",
             "peasant_revenge_player_config_mod_options_set",
             "close_window",
             "{=PRev0094}I must leave now.", null, () => leave_encounter(), 0, null);

            campaignGameStarter.AddDialogLine(
             "peasant_revenge_player_config_mod_npc_end_dis",
             "peasant_revenge_player_config_mod_end_dis",
             "peasant_revenge_player_config_mod_options_set",
             "{=PRev0084}Yes, my {?MAINHERO.GENDER}Lady{?}Lord{\\?}.[rf:idle_happy]",
             () => { StringHelpers.SetCharacterProperties("MAINHERO", Hero.MainHero.CharacterObject); return true; }, null, 200, null);
            campaignGameStarter.AddDialogLine(
             "peasant_revenge_player_config_mod_npc_end_en",
             "peasant_revenge_player_config_mod_end_en",
             "peasant_revenge_player_config_mod_options_set",
             "{=PRev0084}Yes, my {?MAINHERO.GENDER}Lady{?}Lord{\\?}.[rf:idle_angry][ib:closed]",
             () => { StringHelpers.SetCharacterProperties("MAINHERO", Hero.MainHero.CharacterObject); return true; }, null, 200, null);

            #endregion

            #region Revenger who cannot start yet or finished the quest

            //When revenge is ended or does not exist
            campaignGameStarter.AddDialogLine(
            "peasant_revenge_any_revenger_start_ended_revenge",
            "start",
            "peasant_revenge_any_revenger_stop_options",
            "{COMMENT_REVENGE_END}",
            new ConversationSentence.OnConditionDelegate(this.peasant_revenge_revenger_start_ended_fuse_condition),
            () => { leave_encounter(); }, 200, null);
            //When revenge started and is in begin state
            campaignGameStarter.AddDialogLine(
            "peasant_revenge_any_revenger_start",
            "start",
            "peasant_revenge_any_revenger_stop_options",
            "{COMMENT_REVENGE_START}",
            new ConversationSentence.OnConditionDelegate(this.peasant_revenge_revenger_start_fuse_condition),
            null,
            200, null);

            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_any_revenger_stop_option_1a",
             "peasant_revenge_any_revenger_stop_options",
             "peasant_revenge_any_revenger_stop_option_or_else",
             "{=PRev0107}You should drop your revenge, or else...",
             () => { return currentRevenge.state == PeasantRevengeData.quest_state.begin; },
             null,
             110, null);

            campaignGameStarter.AddPlayerLine(
            "peasant_revenge_any_revenger_stop_option_1b",
            "peasant_revenge_any_revenger_stop_options",
            "peasant_revenge_any_revenger_stop_option_or_else",
            "{=PRev0095}There is something I'd like to discuss.",
            () => { return currentRevenge.state > PeasantRevengeData.quest_state.start; },
            null,
            110, null);

            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_any_revenger_stop_option_2",
             "peasant_revenge_any_revenger_stop_options",
             "close_window",
             "{=PRev0094}I must leave now.",
             null,
             () => { leave_encounter(); },
             100, null);

            campaignGameStarter.AddDialogLine(
              "peasant_revenge_any_revenger_or_else",
              "peasant_revenge_any_revenger_stop_option_or_else",
              "peasant_revenge_any_revenger_stop_options_or_else",
              "{=PRev0108}What else?[rf:idle_angry][ib:closed][if:idle_angry]", null, null, 200, null);

            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_any_revenger_or_else_0",
             "peasant_revenge_any_revenger_stop_options_or_else",
             "close_window",
             "{=PRev0109}I will chop your head off!",
             null,
             () =>
             {
                 currentRevenge.Stop();
                 peasant_revenge_peasant_kill_by_hero(Hero.MainHero);
                 currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.notable_killed);
                 leave_encounter();
                 //currentRevenge.xParty.RemoveParty(); // if not removed , party will be left, and can be attacked (no crash). removing pary sometimes causes crashes
             },
             100,
             new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_intimidation_clickable_condition));

            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_any_revenger_or_else_2",
             "peasant_revenge_any_revenger_stop_options_or_else",
             "close_window",
             "{=PRev0110}Go back to your village!",
             () => { return currentRevenge.state == PeasantRevengeData.quest_state.begin; },
             () =>
             {
                 ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentRevenge.executioner.HeroObject, Hero.MainHero, _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge, _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge != 0);
                 currentRevenge.Stop();
                 currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.notable_interrupted);
                 leave_encounter();

             },
             100,
             new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_intimidation_clickable_condition));
            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_any_revenger_or_else_1",
             "peasant_revenge_any_revenger_stop_options_or_else",
             "close_window",
             "{=PRev0083}Nevermind.",
             null,
             () => { leave_encounter(); },
             100, null);
            #endregion

            #region When player is captured as criminal
            campaignGameStarter.AddDialogLine(
                "peasant_revenge_lord_start_grievance",
                "start",
                "peasant_revenge_lord_start_grievance_received",
                "{=PRev0001}You looted nearby village. Peasants demand to cut someone's head off. What will you say?[rf:idle_angry][ib:closed][if:idle_angry]",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_lord_start_condition), null, 100, null);
            campaignGameStarter.AddDialogLine(
                "peasant_revenge_lord_start_grievance",
                "start",
                "peasant_revenge_lord_start_grievance_received",
                "{=PRev0002}Just curious, the {PEASANTREVENGER.LINK} says that you looted your own village earlier. Peasant want your head off. What will you say?[if:convo_thinking][if:idle_happy]",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_lord_start_condition_betray), null, 100, null);

            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_lord_start_grievance_requested_ask_if_not_pay",
               "peasant_revenge_lord_start_grievance_received",
               "peasant_revenge_lord_start_grievance_requested_if_not_pay_options",
               "{=PRev0062}And what if I'll not pay?",
               null,
               null, 110, null, null);

            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_lord_start_grievance_requested_ask_if_not_pay",
               "peasant_revenge_lord_start_grievance_received",
               "peasant_revenge_lord_grievance_received_pay",
               "{=PRev0065}I have friends, who will pay the reparation.",
               null,
               () => peasant_revenge_criminal_has_suporters_consequence(), 110,
               new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_criminal_has_suporters_clickable_condition), null);

            campaignGameStarter.AddDialogLine(
            "peasant_revenge_lord_start_grievance_requested_if_not_pay_options_die",
            "peasant_revenge_lord_start_grievance_requested_if_not_pay_options",
            "peasant_revenge_lord_start_grievance_received",
            "{=PRev0063}Peasant will have your head.[if:convo_thinking][rf:convo_grave][ib:closed]",
            () => { return (Hero.MainHero.CanDie(KillCharacterAction.KillCharacterActionDetail.Executed) && will_party_leader_kill_the_criminal()); },
            null, 100, null);

            campaignGameStarter.AddDialogLine(
            "peasant_revenge_lord_start_grievance_requested_if_not_pay_options_live",
            "peasant_revenge_lord_start_grievance_requested_if_not_pay_options",
            "peasant_revenge_lord_start_grievance_received",
            "{=PRev0064}You will be fine.[if:convo_happy][if:convo_thinking][ib:closed]",
            () => !(Hero.MainHero.CanDie(KillCharacterAction.KillCharacterActionDetail.Executed) && will_party_leader_kill_the_criminal()),
            null, 100, null);

            campaignGameStarter.AddPlayerLine(
                "peasant_revenge_lord_start_grievance_requested",
                "peasant_revenge_lord_start_grievance_received",
                "peasant_revenge_lord_grievance_barter_reaction", "{=PRev0003}I'll pay {REPARATION}{GOLD_ICON}.",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_lord_start_reparation_condition),
                null, 100, null, null);

            campaignGameStarter.AddPlayerLine(
                "peasant_revenge_lord_start_grievance_requested_no",
                "peasant_revenge_lord_start_grievance_received",
                "peasant_revenge_lord_start_grievance_denied_pay", "{=PRev0004}I'll not pay.",
                () => !this.peasant_revenge_lord_start_condition_betray(),
                null, 100, null, null);

            campaignGameStarter.AddPlayerLine(
                "peasant_revenge_lord_start_grievance_requested_no_betray",
                "peasant_revenge_lord_start_grievance_received",
                "peasant_revenge_lord_start_grievance_denied_pay", "{=PRev0005}I'll not pay to this rat.",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_lord_start_condition_betray),
                null, 100, null, null);



            campaignGameStarter.AddPlayerLine(
                "peasant_revenge_lord_start_grievance_requested_no_lie",
                "peasant_revenge_lord_start_grievance_received",
                "peasant_revenge_lord_start_grievance_denied_confirm_lie", "{=PRev0006}It was all {COMPANION.NAME}'s plan! (lie)",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_lord_start_condition_lie),
                null, 100, null, null);

            campaignGameStarter.AddDialogLine(
             "peasant_revenge_lord_start_grievance_denied_confirm",
             "peasant_revenge_lord_start_grievance_denied_confirm_lie",
             "peasant_revenge_lord_start_grievance_denied_confirm_a_lie",
             "{=PRev0007}Are you sure it was as you say?[if:convo_thinking][ib:closed]", null,
             null, 100, null);

            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_lord_start_grievance_denied_not_confirmed_lied",
              "peasant_revenge_lord_start_grievance_denied_confirm_a_lie",
              "peasant_revenge_lord_start_grievance_requested_if_not_pay_options",
              "{=PRev0008}No!", null,
              null, 100, null, null);
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_lord_start_grievance_denied_confirmed_lied",
              "peasant_revenge_lord_start_grievance_denied_confirm_a_lie",
              "peasant_revenge_lord_start_grievance_denied_confirm_lie_ai_decision",
              "{=PRev0009}Yes!", null,
              null, 100, null, null);

            campaignGameStarter.AddDialogLine(
             "peasant_revenge_lord_start_grievance_denied_confirm_a_lie_option_0",
             "peasant_revenge_lord_start_grievance_denied_confirm_lie_ai_decision",
             "peasant_revenge_lord_start_grievance_denied_pay_end_pl_c",
             "{=PRev0100}I cannot make the decision...[if:convo_thinking][ib:closed]",
             () => AIwillMakeNoDecisionDueConflict(Hero.MainHero, currentRevenge),
             () => { currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.party_no_decision); }, 100, null);

            campaignGameStarter.AddDialogLine(
             "peasant_revenge_lord_start_grievance_denied_confirm_a_lie_option_1",
             "peasant_revenge_lord_start_grievance_denied_confirm_lie_ai_decision",
             "peasant_revenge_lord_start_grievance_denied_pay_end_pl_c",
             "{=PRev0101}So be it[ib:closed]",
             () =>
             {
                 bool kill_both = CheckConditions(currentRevenge.party.Owner, currentRevenge.accused_hero.HeroObject, _cfg.values.ai.lordWillKillBothAccusedHeroAndCriminalLord);
                 return !AIwillMakeNoDecisionDueConflict(Hero.MainHero, currentRevenge) && !kill_both;
             }, () => { currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.accused_hero_killed); }, 100, null);

            campaignGameStarter.AddDialogLine(
             "peasant_revenge_lord_start_grievance_denied_confirm_a_lie_option_2",
             "peasant_revenge_lord_start_grievance_denied_confirm_lie_ai_decision",
             "peasant_revenge_lord_start_grievance_denied_pay_end_pl_c",
             "{=PRev0077}You both deserve peasant revenge![rf:idle_angry][ib:closed]",
             () =>
             {
                 bool kill_both = CheckConditions(currentRevenge.party.Owner, currentRevenge.accused_hero.HeroObject, _cfg.values.ai.lordWillKillBothAccusedHeroAndCriminalLord);
                 return !AIwillMakeNoDecisionDueConflict(Hero.MainHero, currentRevenge) && kill_both;
             },
             () =>
             {
                 currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.criminal_killed);
                 currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.accused_hero_killed);
             }, 100, null);

            campaignGameStarter.AddDialogLine(
             "peasant_revenge_lord_start_grievance_denied_pay_end",
             "peasant_revenge_lord_start_grievance_denied_pay",
             "peasant_revenge_lord_start_grievance_denied_pay_end_pl_c",
             "{=PRev0010}Well, maybe it is not for peasant to decide your fate...[if:convo_thinking]",
             () => !(Hero.MainHero.CanDie(KillCharacterAction.KillCharacterActionDetail.Executed) && will_party_leader_kill_the_criminal()),
             () =>
             {
                 currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.party_denied);
                 ChangeRelationAction.ApplyPlayerRelation(currentRevenge.executioner.HeroObject, _cfg.values.relationChangeWhenCriminalRefusedToPayReparations, true, true);
                 ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, currentRevenge.executioner.HeroObject, _cfg.values.relationChangeWhenCannotPayReparations, false);
             }, 100, null);

            campaignGameStarter.AddDialogLine(
            "peasant_revenge_lord_start_grievance_denied_pay_end",
            "peasant_revenge_lord_start_grievance_denied_pay",
            "peasant_revenge_lord_start_grievance_denied_pay_end_pl_c",
            "{=PRev0012}Well I'm happy with that.[ib:happy]",
            () => { return (Hero.MainHero.CanDie(KillCharacterAction.KillCharacterActionDetail.Executed) && will_party_leader_kill_the_criminal()); },
            () => { currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.criminal_killed); }, 100, null);

            campaignGameStarter.AddPlayerLine(
            "peasant_revenge_lord_start_grievance_denied_pay_end_comment",
            "peasant_revenge_lord_start_grievance_denied_pay_end_pl_c",
            "close_window",
            "{PLCOMMENT}",
            () =>
            {
                TextObject text = new TextObject("{=PRev0106}...");
                if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.party_no_decision))
                {
                    text = new TextObject("{=PRev0102}A good decision...");
                }
                else
                {
                    text = new TextObject("{=PRev0106}...");
                }
                MBTextManager.SetTextVariable("PLCOMMENT", text);
                return true;
            },
            () =>
            {
                if ((currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) &&
                   currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed)))
                {
                    peasant_revenge_peasant_kill_both_consequence_lied();
                }
                else
                {
                    if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
                    {
                        peasant_revenge_cannot_pay_consequence();
                    }
                    else if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed))
                    {
                        peasant_revenge_peasant_kill_victim_consequence_lied();
                    }
                }
                if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.party_no_decision))
                {
                    peasant_revenge_hero_cannot_make_decision_consequence(currentRevenge.party.LeaderHero);
                }
                currentRevenge.Stop();
            }, 100, null, null);

            campaignGameStarter.AddDialogLine(
             "peasant_revenge_lord_grievance_barter_reaction_line",
             "peasant_revenge_lord_grievance_barter_reaction",
             "peasant_revenge_lord_grievance_wait_pay_barter_line",
             "{=PRev0011}Pay for your crime.[rf:idle_angry][if:convo_bored]",
              null,
              null, 100, null);
            campaignGameStarter.AddDialogLine(
             "peasant_revenge_lord_grievance_pay_barter_line",
             "peasant_revenge_lord_grievance_wait_pay_barter_line",
             "peasant_revenge_lord_grievance_received_pay",
             "{=!}BARTER LINE - Covered by barter interface. Please do not remove these lines!",
             null,
             new ConversationSentence.OnConsequenceDelegate(this.peasant_revenge_player_barter_consequence), 100, null);
            campaignGameStarter.AddDialogLine(
             "peasant_revenge_lord_start_grievance_received_pay",
             "peasant_revenge_lord_grievance_received_pay",
             "close_window",
             "{=PRev0012}Well I'm happy with that.[ib:happy]",
             new ConversationSentence.OnConditionDelegate(this.barter_successful_condition),
             new ConversationSentence.OnConsequenceDelegate(peasant_revenge_player_payed_consecuence), 100, null);
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_lord_start_grievance_not_received_pay",
              "peasant_revenge_lord_grievance_received_pay",
              "peasant_revenge_lord_start_grievance_received",
              "{=PRev0013}Well, that is unfortunate.[ib:warrior][if:convo_bored][rf:convo_grave]", () => !this.barter_successful_condition(),
              null, 100, null);

            #endregion

            #region When player captured the criminal
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_start_grievance",
               "start",
               "peasant_revenge_peasants_start_grievance_received",
               "{=PRev0014}Your prisoner {CRIMINAL.LINK} looted our village. We demand criminal's head on spike! What will you say?[if:convo_furious][ib:aggressive]",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_peasant_start_condition), null, 120, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_start_grievance_requested_die",
               "peasant_revenge_peasants_start_grievance_received",
               "peasant_revenge_peasants_finish_criminal_comment",
               "{=PRev0015}{CRIMINAL.NAME} will die.",
               null,
               () => { currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.criminal_killed); }, 100, null, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_start_grievance_requested_pay",
               "peasant_revenge_peasants_start_grievance_received",
               "peasant_revenge_peasants_finish_paid",
               "{=PRev0016}{CRIMINAL.NAME} will pay {REPARATION}{GOLD_ICON}.",
               new ConversationSentence.OnConditionDelegate(this.criminal_has_enougth_gold_condition),
               new ConversationSentence.OnConsequenceDelegate(this.criminal_has_to_pay_in_gold_consequence), 90, null, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_start_grievance_requested_not_bussiness",
               "peasant_revenge_peasants_start_grievance_received",
               "peasant_revenge_peasants_finish_denied",
               "{=PRev0017}No, it is not your business, peasant!", null,
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_peasant_not_kill_hero_consequence), 90, null, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_start_grievance_requested_ask_criminal",
               "peasant_revenge_peasants_start_grievance_received",
               "close_window",
               "{=PRev0071}What does the criminal say about it?",
               new ConversationSentence.OnConditionDelegate(have_accused_hero),
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_criminal_blaming_consequence), 80, null, null);
            campaignGameStarter.AddPlayerLine(
                "peasant_revenge_peasants_start_grievance_requested_not_now",
                "peasant_revenge_peasants_start_grievance_received",
                "close_window",
                "{=PRev0119}Not now.",
                () => { return currentRevenge.xParty != null; },
                () => { leave_encounter(); },
                70, null);
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_peasants_ask_criminal_start_explain",
              "start",
              "peasant_revenge_peasants_ask_criminal_options_start",
              "{=PRev0073}I can swear! It was all {CVICTIM.LINK}'s plan![rf:convo_grave][ib:closed]",
              new ConversationSentence.OnConditionDelegate(peasant_revenge_ask_criminal_start_condition),
              null,
              120, null);
            campaignGameStarter.AddDialogLine(
             "peasant_revenge_peasants_ask_criminal_explain",
             "peasant_revenge_peasants_ask_criminal_options_start",
             "peasant_revenge_peasants_ask_criminal_options",
             "{=PRev0074}{CVICTIM.LINK} is the criminal.[rf:convo_angry][ib:closed]",
             () =>
             {
                 StringHelpers.SetCharacterProperties("CVICTIM", currentRevenge.accused_hero);
                 return true;
             }, () => { currentRevenge.Can_peasant_revenge_accuser_lord_start = false; }, 110, null);
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_peasants_ask_criminal_option_0",
              "peasant_revenge_peasants_ask_criminal_options",
              "peasant_revenge_peasants_finish_criminal_comment",
              "{=PRev0075}I believe you.",
              null, () => { currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.accused_hero_killed); }, 90, null, null);
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_peasants_ask_criminal_option_1",
              "peasant_revenge_peasants_ask_criminal_options",
              "peasant_revenge_peasants_finish_criminal_comment",
              "{=PRev0076}You are lying.",
              null, () => { currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.criminal_killed); }, 90, null, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_ask_criminal_option_2",
               "peasant_revenge_peasants_ask_criminal_options",
               "peasant_revenge_peasants_finish_criminal_comment",
               "{=PRev0077}You both deserve peasant's revenge!",
               null, () =>
               {
                   currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.accused_hero_killed);
                   currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.criminal_killed);
               }, 90, null, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_ask_criminal_option_2",
               "peasant_revenge_peasants_ask_criminal_options",
               "peasant_revenge_peasants_finish_criminal_comment",
               "{=PRev0098}I cannot make the decision...",
               null, () =>
               {
                   currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.party_no_decision);
                   peasant_revenge_hero_cannot_make_decision_consequence(Hero.MainHero);
               },
               90, null, null);
            campaignGameStarter.AddDialogLine(
             "peasant_revenge_peasants_finish_criminal_killed_end",
             "peasant_revenge_peasants_finish_criminal_comment",
             "peasant_revenge_peasants_finish_criminal_killed_c_pl_options",
             "{=PRev0020}Revenge![if:convo_happy][ib:happy]",
             () => { return (currentRevenge.executioner.HeroObject == Hero.OneToOneConversationHero); },
             null, 120, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_finish_criminal_comment_pos_end",
               "peasant_revenge_peasants_finish_criminal_comment",
               "peasant_revenge_peasants_finish_criminal_killed_c_pl_options",
               "{=PRev0099}A good decision...[if:convo_happy][ib:happy]",
               () => { return !currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed) && currentRevenge.criminal.HeroObject == Hero.OneToOneConversationHero; }, null, 120, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_finish_criminal_comment_neg_end",
               "peasant_revenge_peasants_finish_criminal_comment",
               "peasant_revenge_peasants_finish_criminal_killed_c_pl_options",
               "{=PRev0018}But, but...[ib:closed][if:convo_shocked][if:convo_astonished]",
               () => { return currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed) && currentRevenge.criminal.HeroObject == Hero.OneToOneConversationHero; }, null, 120, null);
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_player_demand_lost_ransom_leave",
              "peasant_revenge_peasants_finish_criminal_killed_c_pl_options",
              "close_window",
              "{=PRev0094}I must leave now.",
              null, () =>
              {
                  if ((currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) &&
                    currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed)))
                  {
                      peasant_revenge_peasant_messenger_kill_both_consequence();
                  }
                  else
                  {
                      if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
                      {
                          peasant_revenge_peasant_kill_the_criminal();
                      }
                      else if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed))
                      {
                          peasant_revenge_peasant_messenger_kill_victim_consequence();
                      }
                  }
                  if ((currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) ||
                    currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed)))
                  {
                      peasant_revenge_leave_lord_body_consequence();
                  }
                  currentRevenge.Stop();
                  leave_encounter();
              }, 100, null, null);
            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_player_demand_lost_ransom_take_c_body_ransom",
             "peasant_revenge_peasants_finish_criminal_killed_c_pl_options",
             "close_window",
             "{=PRev0105}I'll take the remains.",
             () =>
             {
                 return (
                    (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) ||
                    currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
                    );
             }, () =>
             {
                 if ((currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) &&
                    currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed)))
                 {
                     peasant_revenge_peasant_messenger_kill_both_consequence();
                     AddKilledLordsCorpses(currentRevenge);
                     peasant_revenge_player_demand_ransom_consequence(currentRevenge.criminal.HeroObject);
                     peasant_revenge_player_demand_ransom_consequence(currentRevenge.accused_hero.HeroObject);

                 }
                 else
                 {
                     if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
                     {
                         peasant_revenge_peasant_kill_the_criminal();
                         AddKilledLordsCorpses(currentRevenge);
                         peasant_revenge_player_demand_ransom_consequence(currentRevenge.criminal.HeroObject);
                     }
                     else if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed))
                     {
                         peasant_revenge_peasant_messenger_kill_victim_consequence();
                         AddKilledLordsCorpses(currentRevenge);
                         peasant_revenge_player_demand_ransom_consequence(currentRevenge.accused_hero.HeroObject);
                     }
                 }
                 currentRevenge.Stop();
                 leave_encounter();
             }, 90, null, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_finish_denied_end",
               "peasant_revenge_peasants_finish_denied",
               "close_window",
               "{=PRev0018}But, but...[ib:closed][if:convo_bared_teeth]", null, () => leave_encounter(), 120, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_finish_paid_end",
               "peasant_revenge_peasants_finish_paid",
               "close_window",
               "{=PRev0019}Better than nothing...[ib:closed][if:idle_normal]", null, () => leave_encounter(), 120, null);
            #endregion

            #region When hero (from player clan/kingdom) cannot pay , and maybe player can pay the reparation
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_messenger_start_grievance",
               "start",
               "peasant_revenge_peasants_messenger_start_grievance_received",
               "{PEASANTDEMANDS}",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_peasant_messenger_start_condition), null, 120, null);

            //will pay
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_peasants_messenger_start_grievance_received_pay",
              "peasant_revenge_peasants_messenger_start_grievance_received",
              "peasant_revenge_peasants_messenger_finish_paid",
              "{=PRev0003}I'll pay {REPARATION}{GOLD_ICON}.",
              new ConversationSentence.OnConditionDelegate(this.player_has_enougth_gold_condition),
              new ConversationSentence.OnConsequenceDelegate(this.player_pay_messenger_in_gold_consequence), 120, null, null);
            //will not pay wait ransom (should be only when criminal is not enemy. enemy should decide by his traits, persuation from player, relations...)
            //this dialog should add peasant dialog lines where he chooses to kill or not ... will forse player to look what lord traits are...
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_messenger_start_grievance_received_not_pay_not_kill",
               "peasant_revenge_peasants_messenger_start_grievance_received",
               "close_window",
               "{=PRev0022}I'll not bow to peasant demands! And {HERO.NAME} should too!",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_peasant_messenger_fill_hero_condition),
               () => { peasant_revenge_peasant_messenger_not_kill_hero_consequence(); leave_encounter(); }, 110, null, null);
            //will not pay peasant go to criminal hero clan/kingdom to ask reparation
#if false
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_messenger_start_grievance_received_not_pay_let_save",
               "peasant_revenge_peasants_messenger_start_grievance_received",
               "peasant_revenge_peasants_messenger_not_pay_let_save",
               "{=PRev0047}I'll let {?CRIMINAL.GENDER}Lady{?}Lord{\\?} {CRIMINAL.NAME} to save {?CRIMINAL.GENDER}herself{?}himself{\\?}...",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_peasant_messenger_not_pay_let_save_condition),
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_peasant_messenger_not_pay_let_save_consequence), 105, null, null);
#endif
            //will not pay
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_messenger_start_grievance_received_not_pay",
               "peasant_revenge_peasants_messenger_start_grievance_received",
               "peasant_revenge_peasants_messenger_finish_not_paid",
               "{=PRev0023}I'll not pay! Criminal {CVICTIM.NAME} can die!", () =>
               {
                   if (currentRevenge.accused_hero != null)
                   {
                       StringHelpers.SetCharacterProperties("CVICTIM", currentRevenge.accused_hero);
                   }
                   else
                   {
                       StringHelpers.SetCharacterProperties("CVICTIM", currentRevenge.criminal);
                   }
                   return true;
               },
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_peasant_messenger_kill_victim_consequence), 100, null, null);
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_peasants_messenger_start_grievance_received_not_pay_both_kill",
              "peasant_revenge_peasants_messenger_start_grievance_received",
              "peasant_revenge_peasants_messenger_finish_not_paid",
              "{=PRev0070}They are both criminals!", () => have_accused_hero(),
              new ConversationSentence.OnConsequenceDelegate(peasant_revenge_peasant_messenger_kill_both_consequence), 100, null, null);
            //not pay and kill messenger
            campaignGameStarter.AddPlayerLine(
                "peasant_revenge_peasants_messenger_start_grievance_received_kill_messenger",
                "peasant_revenge_peasants_messenger_start_grievance_received",
                "peasant_revenge_peasants_finish_denied_and_killed",
                "{=PRev0024}How dare you! You'll die!",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_peasant_messenger_killed_condition),
                null, 90, null, null);
            campaignGameStarter.AddPlayerLine(
                "peasant_revenge_peasants_messenger_grievance_requested_not_now",
                "peasant_revenge_peasants_messenger_start_grievance_received",
                "close_window",
                "{=PRev0119}Not now.",
                () => { return currentRevenge.xParty != null; },
                () => { leave_encounter(); },
                70, null);
            campaignGameStarter.AddDialogLine(
             "peasant_revenge_peasants_messenger_finish_paid_end",
             "peasant_revenge_peasants_messenger_finish_paid",
             "close_window",
             "{=PRev0025}Better than nothing...[ib:closed][if:idle_angry][rf:idle_angry]", null, () => leave_encounter(), 120, null);

            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_finish_denied_and_killed_end",
               "peasant_revenge_peasants_finish_denied_and_killed",
               "peasant_revenge_peasants_finish_denied_and_killed_player_line",
               "{=PRev0026}But, but...[if:convo_furious][ib:convo_bared_teeth][ib:aggressive][if:convo_astonished]", null, null, 120, null);
            campaignGameStarter.AddPlayerLine(
                "peasant_revenge_peasants_finish_denied_and_killed_player_line_end",
                "peasant_revenge_peasants_finish_denied_and_killed_player_line",
                "close_window",
                "{=PRev0027}And I'll send your head to {HERO.NAME}!",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_peasant_messenger_killed_condition),
                () => { peasant_revenge_peasant_messenger_killed_consequence(); leave_encounter(); }, 90, null, null);

            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_finish_not_paid_with_compensation",
               "peasant_revenge_peasants_messenger_finish_not_paid",
               "close_window",
               "{=PRev0028}So, criminal will die.[ib:closed][if:happy]",
               new ConversationSentence.OnConditionDelegate(peasant_revenge_party_need_compensation_for_killed_pow_condition),
               () => { peasant_revenge_party_need_compensation_for_killed_pow_consequence(); leave_encounter(); }, 120, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_finish_not_paid_no_compensation",
               "peasant_revenge_peasants_messenger_finish_not_paid",
               "close_window",
               "{=PRev0029}Criminal lord is dead![ib:closed][if:happy]",
                () => !peasant_revenge_party_need_compensation_for_killed_pow_condition(),
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_end_revenge_consequence), 120, null);
            #endregion

            #region Prisoner party demands compensation because no ransom
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_party_need_compensation_start",
               "start",
               "peasant_revenge_party_need_compensation_ask_support",
               "{=PRev0030}Look, the peasant killed our prisoner![ib:convo_bared_teeth][if:convo_shocked][if:convo_astonished]",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_party_need_compensation_condition),
               null, 120, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_party_need_compensation_support",
               "peasant_revenge_party_need_compensation_ask_support",
               "peasant_revenge_party_need_compensation_player_options",
               "{=PRev0031}{?GIFT_RECEIVER.GENDER}Lady{?}Lord{\\?} {GIFT_RECEIVER.LINK} is asking you for ransom gold {RANSOM_COMPENSATION}{GOLD_ICON}.[ib:convo_nervous][if:convo_grave]",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_party_need_compensation_gift_condition),
               null, 120, null);

            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_party_need_compensation_player_options_0",
             "peasant_revenge_party_need_compensation_player_options",
             "peasant_revenge_party_need_compensation_denied",
             "{=PRev0032}I'll not pay anything.",
             null,
             null, 115, null);
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_party_need_compensation_player_options_1",
              "peasant_revenge_party_need_compensation_player_options",
               "peasant_revenge_party_need_compensation_barter_reaction",
              "{=PRev0033}Please, take this gift to {?GIFT_RECEIVER.GENDER}Lady{?}Lord{\\?} {GIFT_RECEIVER.NAME}.",
              new ConversationSentence.OnConditionDelegate(this.peasant_revenge_party_get_compensation_gift_condition), null
              , 110, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_party_need_compensation_player_options_2",
               "peasant_revenge_party_need_compensation_player_options",
               "peasant_revenge_party_need_compensation_denied_party_killed",
               "{=PRev0034}Your ransom is dead and now you will die too!",
               null,
               null, 100, null);

            campaignGameStarter.AddDialogLine(
               "peasant_revenge_party_need_compensation_barter_reaction_line",
               "peasant_revenge_party_need_compensation_barter_reaction",
               "peasant_revenge_party_need_compensation_barter_line",
              "{=PRev0035}A gift?[if:convo_thinking]",
               null,
               null, 100, null);
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_party_need_compensation_open_barter",
              "peasant_revenge_party_need_compensation_barter_line",
              "peasant_revenge_party_need_compensation_received_pay",
             "{=!}BARTER LINE - Covered by barter interface. Please do not remove these lines!",
              null,
              new ConversationSentence.OnConsequenceDelegate(peasant_revenge_party_need_compensation_player_barter_consequence), 120, null);

            campaignGameStarter.AddDialogLine(
              "peasant_revenge_party_need_compensation_now_not_received_pay",
              "peasant_revenge_party_need_compensation_received_pay",
              "close_window",
              "{=PRev0036}So you cannot pay...[ib:closed][if:idle_angry]",
              () => !this.barter_successful_condition(),
              new ConversationSentence.OnConsequenceDelegate(peasant_revenge_party_need_compensation_not_payed_consequence), 0, null);

            campaignGameStarter.AddDialogLine(
             "peasant_revenge_party_need_compensation_now_received_pay",
             "peasant_revenge_party_need_compensation_received_pay",
             "close_window",
             "{=PRev0037}We are pleased.[if:convo_happy]",
             new ConversationSentence.OnConditionDelegate(this.barter_successful_condition),
             new ConversationSentence.OnConsequenceDelegate(peasant_revenge_party_need_compensation_payed_consequence), 0, null);
            campaignGameStarter.AddDialogLine(
             "peasant_revenge_party_need_compensation_denied_the_payment",
             "peasant_revenge_party_need_compensation_denied",
             "close_window",
             "{=PRev0038}It is unfair.[ib:closed][if:idle_angry]", null,
             new ConversationSentence.OnConsequenceDelegate(peasant_revenge_party_need_compensation_not_payed_consequence), 0, null);
            campaignGameStarter.AddDialogLine(
                "peasant_revenge_party_need_compensation_denied_the_payment",
                "peasant_revenge_party_need_compensation_denied_party_killed",
                "close_window",
                "{=PRev0039}But I'm the messenger...[ib:convo_bared_teeth][ib:aggressive][if:convo_astonished]", null,
                new ConversationSentence.OnConsequenceDelegate(peasant_revenge_party_need_compensation_not_payed_party_killed_consequence), 0, null);


            #endregion

            #region When player is captured by caravan party as criminal
            //caravan notable: You have a guest. He says he knows you
            //peasant          Pay or die!
            //
            #endregion

            #region When player is captured by bandit party as criminal
            //bandit: somebody paid for your death 

            #endregion

            //Dialogs for mod configuration in game
            #region Peasants has no traits to resist
            #region start
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_ask",
               "hero_main_options",
               "peasant_revenge_player_not_happy_with_peasant_start_options_eset",
               "{=PRev0048}Do you deal with criminals in this village?",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_start_condition),
               null/*() => { SetHeroTraitValue(Hero.MainHero, "Valor", -2); SetHeroTraitValue(Hero.MainHero, "Mercy", 2); }*/
               , 100, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_player_not_happy_with_peasant_start_peasant",
               "peasant_revenge_player_not_happy_with_peasant_start_options_eset",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "{=PRev0049}Yes. Why do you ask?[ib:closed][rf:convo_thinking]",
               () => { return notable_can_do_revenge(); }, null, 100, null);
            campaignGameStarter.AddDialogLine(
                "peasant_revenge_player_not_happy_with_peasant_start_peasant",
                "peasant_revenge_player_not_happy_with_peasant_start_options_eset",
                "peasant_revenge_player_not_happy_with_peasant_start_options",
                "{=PRev0049}No, I do not care.[ib:closed]",
                () => { return !notable_can_do_revenge(); }, null, 100, null);
            #endregion
            #region options
#warning Add crime rating increase if executed noble
            //EXECUTE
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_fast",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
                "close_window",
               "{=PRev0050}The problem is in your head!", null,
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_player_not_happy_with_peasant_chop_consequence)
               , 100, null);
            //TEACH
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_teach",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "peasant_revenge_player_not_happy_with_peasant_post_learned",
               "{PAYER_COMMENT_REVENGE_TEACH}",
               new ConversationSentence.OnConditionDelegate(peasant_revenge_player_not_happy_with_peasant_teach_condition),
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_player_not_happy_with_peasant_teach_consequence), 110,
               new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_start_teach_condition));
            //BRIBE
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_give",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "peasant_revenge_player_not_happy_with_peasant_post_learned",
               "{PAYER_COMMENT_REVENGE_BRIBE}",
               new ConversationSentence.OnConditionDelegate(peasant_revenge_player_not_happy_with_peasant_bribe_condition),
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_player_not_happy_with_peasant_bribe_consequence), 120,
               new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_start_bribe_condition));
            //Leave
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_give_leave",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "close_window",
               "{= PRev0083}Nevermind.",
               null,
               () => leave_encounter(), 80, null);
            #endregion
#warning make dialog strings and relation change if fail. Make it possible to fail the bribe/persuation! Bribing could result in "give money all day doesnt matter..." 
            #region ending
            // CAN REVENGE SUCCESS
            campaignGameStarter.AddDialogLine(
            "peasant_revenge_player_not_happy_with_peasant_learned_1",
            "peasant_revenge_player_not_happy_with_peasant_post_learned",
            "close_window",
            "{=PRev0120}They are breaking the law and they will face consequences for their crimes.[ib:closed][if:angry]",
             () => peasant_revenge_player_not_happy_with_peasant_post_learned_1_on_condition(),
             () => { peasant_revenge_player_not_happy_with_peasant_post_persuation_on_consequence(); leave_encounter(); },
             100, null);
            // CANNOT REVENGE SUCCESS
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_player_not_happy_with_peasant_learned_2",
              "peasant_revenge_player_not_happy_with_peasant_post_learned",
              "close_window",
              "{=PRev0054}They will do what they want. It is not my business to interfere.[ib:closed][if:angry]",
              () => peasant_revenge_player_not_happy_with_peasant_post_learned_2_on_condition(),
              () => { peasant_revenge_player_not_happy_with_peasant_post_persuation_on_consequence(); leave_encounter(); },
              100, null);
            // BOTH NOT SUCCESS
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_player_not_happy_with_peasant_learned_3",
              "peasant_revenge_player_not_happy_with_peasant_post_learned",
              "close_window",
              "{=PRev0117}Enough, I will not change my intentions![ib:closed][if:convo_bared_teeth][if:idle_angry]",
              () => peasant_revenge_player_not_happy_with_peasant_post_learned_3_on_condition(),
              () => { peasant_revenge_player_not_happy_with_peasant_post_persuation_on_consequence(); leave_encounter(); },// this change must be then persuation fail or success //() => { ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, Hero.OneToOneConversationHero, _cfg.values.relationChangeWhenLordTeachPeasant, true); },
              100, null) ;
            // PERSUATION FAIL
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_player_not_happy_with_peasant_learned_4",
              "peasant_revenge_player_not_happy_with_peasant_post_learned",
              "close_window",
              "{=PRev0121}I can't make any promises, but I'll see what I can do..[ib:closed]",
              () => peasant_revenge_player_not_happy_with_peasant_post_learned_4_on_condition(),
              () => { peasant_revenge_player_not_happy_with_peasant_post_persuation_on_consequence(); leave_encounter(); },
              100, null);
            #endregion
            #endregion

        }
        #region peasant revenge persuede

        #region persuation task
        private void AddPersuasionDialogs(DialogFlow dialog)
        {
            dialog.AddDialogLine("peasant_revenge_player_not_happy_with_peasant_learn_accepted",
                "start_peasant_revenger_persuasion",
                "lesser_party_persuasion_start_reservation",
                "{=FwtFtpwp}How?", 
                null, 
                new ConversationSentence.OnConsequenceDelegate(this.persuasion_start_with_lesser_party_on_consequence),
                this, 100, null, null, null);

            dialog.AddDialogLine("lesser_party_persuasion_attempt",
                "peasant_revenge_persuasion_start_reservation",
                "peasant_revenge_persuasion_select_option",
                "{=wM77S68a}What's there to discuss?",
                null,
                null, this, 100, null, null, null);
        }

        private void persuasion_start_with_lesser_party_on_consequence()
        {
            ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.MediumHard);
        }
      
        private PersuasionTask GetPersuasionTask()
        {
            PersuasionTask persuasionTask = new PersuasionTask(0);
            persuasionTask.FinalFailLine = new TextObject("{=PRev0121}I can't make any promises, but I'll see what I can do..[if:convo_furious][ib:aggressive]", null);
            persuasionTask.TryLaterLine = new TextObject("{=PRev0117}Enough, I will not change my intentions![ib:closed][if:convo_bared_teeth][if:idle_angry]", null);
            persuasionTask.SpokenLine = new TextObject("{=6P1ruzsC}Maybe...", null);
            PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.Easy,
                false, new TextObject("{=*}I'm not afraid of these criminals.", null), null, false, false, false);
            persuasionTask.AddOptionToTask(option);
            PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Engineering, DefaultTraits.Mercy, TraitEffect.Positive, PersuasionArgumentStrength.Normal,
                false, new TextObject("{=*}Someone must be held accountable for destruction of our villages.", null), null, false, false, false);
            persuasionTask.AddOptionToTask(option2);
            PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, PersuasionArgumentStrength.VeryHard,
                false, new TextObject("{=*}Give justice into hands of nobles. You are not so important.", null), null, false, false, false);
            persuasionTask.AddOptionToTask(option3);
            return persuasionTask;
        }

        #endregion

        private void peasant_revenge_player_not_happy_with_peasant_post_persuation_on_consequence()
        {
            bool can_revenge = notable_can_do_revenge();

            if (persuade_status == persuade_type.teach)
            {
                if (previous_can_revenge)
                {
                    OnLordPersuedeNotableNotToRevenge(Hero.MainHero);
                }
                else
                {
                    OnLordPersuedeNotableToRevenge(Hero.MainHero);
                }
            }
            else if (persuade_status == persuade_type.bribe)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, Hero.MainHero,
              get_notable_persuaded_count() > 2 ? -_cfg.values.relationChangeWhenLordBribePeasant: _cfg.values.relationChangeWhenLordBribePeasant, _cfg.values.relationChangeWhenLordBribePeasant != 0);
            }
            persuade_status = persuade_type.none;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_1_on_condition()
        {
            bool can_revenge = notable_can_do_revenge();

            if (can_revenge && get_notable_persuaded_count() <= 2 
                && can_revenge != previous_can_revenge)
            {
                return true;
            }
            return false;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_2_on_condition()
        {
            bool cannot_revenge = !notable_can_do_revenge();

            if (cannot_revenge && get_notable_persuaded_count() <= 2 && 
                cannot_revenge != !previous_can_revenge)
            {
                return true;
            }
            return false;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_3_on_condition()
        {

            return get_notable_persuaded_count() > 2;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_4_on_condition()
        {

            bool can_revenge = notable_can_do_revenge();
            if (get_notable_persuaded_count() <= 2 &&
                can_revenge == previous_can_revenge)
            {
                return true;
            }

            return false;
        }

        private void add_notable_persuaded_count()
        {
            if (Hero.OneToOneConversationHero == null) return;

            var pdata = persuadedHeroData.Where((x) => x.Id.Equals(Hero.OneToOneConversationHero.StringId)).FirstOrDefault();

            if (pdata != null)
            {
                pdata.persuade_try_count++;
            }
            else
            {
                persuadedHeroData.Add(new PersuadedHeroData { Id = Hero.OneToOneConversationHero.StringId, persuade_try_count = 1 });
            }
        }

        private uint get_notable_persuaded_count()
        {
            if (Hero.OneToOneConversationHero == null) return 0;

            var pdata = persuadedHeroData.Where((x) => x.Id.Equals(Hero.OneToOneConversationHero.StringId)).FirstOrDefault();

            return pdata != null ? pdata.persuade_try_count : 0;
        }

#warning make bribe barterable or persuede amount of money? Make bribe dependent on relation, traits...
        private void peasant_revenge_player_not_happy_with_peasant_bribe_consequence()
        {
            persuade_status = persuade_type.bribe;
            add_notable_persuaded_count();
            if (get_notable_persuaded_count() > 2) return;

            int bribe_percents = _cfg.values.goldPercentOfPeasantTotallGoldToTeachPeasantToBeLoyal;
            int bribe = Hero.OneToOneConversationHero.Gold * bribe_percents / 100;
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, bribe);
            if (CheckConditions(Hero.MainHero, Hero.OneToOneConversationHero, _cfg.values.ai.notableWillAcceptTheBribe))
            {
                TeachHeroTraits(Hero.OneToOneConversationHero, _cfg.values.peasantRevengerExcludeTrait, notable_can_do_revenge());
            }
        }

        // TODO: make persuation here
        private bool peasant_revenge_player_not_happy_with_peasant_bribe_condition()
        {
            // string msg = (notable_can_do_revenge() ? "{=PRev0117}Here take {BRIBEVALUE}{GOLD_ICON}. Close your eyes at crimes of the noble people." : "{=PRev0052}Here take {BRIBEVALUE}{GOLD_ICON}. Make criminals pay for their crimes!");
            string msg = (notable_can_do_revenge() ? "{=PRev0118}Here take {BRIBEVALUE}{GOLD_ICON}. Noble people are not the criminals for you." : "{=PRev0052}Here take {BRIBEVALUE}{GOLD_ICON}. Make criminals pay for their crimes!");

            // TODO: make bribe value vary (look at skills, relations...)
            TextObject textObject = new TextObject(msg, null);

            int bribe = Hero.OneToOneConversationHero.Gold * _cfg.values.goldPercentOfPeasantTotallGoldToTeachPeasantToBeLoyal / 100;

            textObject.SetTextVariable("BRIBEVALUE", bribe);

            MBTextManager.SetTextVariable("PAYER_COMMENT_REVENGE_BRIBE", textObject);

            bool can_hero_pay_the_bribe = Hero.MainHero.Gold >= bribe;

            return can_hero_pay_the_bribe;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_teach_condition()
        {
            string msg = "{=PRev0051}I can try to teach you by my example...";

            //int honor = GetHeroTraitValue(Hero.OneToOneConversationHero, "Honor");
            //int generosity = GetHeroTraitValue(Hero.OneToOneConversationHero, "Generosity");

            if (notable_can_do_revenge())
            {
                msg = "{=PRev0117}Great question! Let me show my example...";
            }

            PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Valor, TraitEffect.Positive, PersuasionArgumentStrength.Easy,
               false, new TextObject("{=*}I'm not afraid of these criminals.", null), null, false, false, false);
            
            TextObject textObject = new TextObject(msg, null);
            textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(option, true));
            MBTextManager.SetTextVariable("PAYER_COMMENT_REVENGE_TEACH", textObject);
            return true;
        }

        private bool notable_can_do_revenge()
        {
            return !hero_trait_list_condition(Hero.OneToOneConversationHero, _cfg.values.peasantRevengerExcludeTrait);
        }
        private bool peasant_revenge_player_not_happy_with_peasant_start_bribe_condition(out TextObject text)
        {
            bool will_accept_bribe = CheckConditions(Hero.MainHero, Hero.OneToOneConversationHero, _cfg.values.ai.notableWillAcceptTheBribe);

            text = TextObject.Empty;

            if (!will_accept_bribe)
            {
                text = new TextObject("{=PRev0122}Cannot bribe.");
            }
            return true;
        }
        private bool peasant_revenge_player_not_happy_with_peasant_start_teach_condition(out TextObject text)
        {

            bool can_revenge_have_no_ex_traits = notable_can_do_revenge() && hero_trait_list_condition(Hero.MainHero, _cfg.values.peasantRevengerExcludeTrait);
            bool cannot_revenge_have_ex_traits = !notable_can_do_revenge() && !hero_trait_list_condition(Hero.MainHero, _cfg.values.peasantRevengerExcludeTrait);

            bool start = Hero.OneToOneConversationHero != null && (can_revenge_have_no_ex_traits || cannot_revenge_have_ex_traits);
              
#warning do probability - no hard no. Should be persuation...fun part. Bribing should make notable be dependant on bribe . If he is not bribed again he should be angry...
            text = TextObject.Empty;

            if (!start)
            {
               text = new TextObject("{=PRev0055}Do not have needed traits");
            }
            // return start;
            return true;
        }

        private void peasant_revenge_player_not_happy_with_peasant_teach_consequence()
        {
            persuade_status = persuade_type.teach;            
            add_notable_persuaded_count();                                        // 
            if (get_notable_persuaded_count() > 2)
                return;
            TeachHeroTraits(Hero.OneToOneConversationHero, _cfg.values.peasantRevengerExcludeTrait, previous_can_revenge, Hero.MainHero);
        }
        private void peasant_revenge_player_not_happy_with_peasant_chop_consequence()
        {
            bool chop_purpose = notable_can_do_revenge(); // true if notable can do the revenge, but hero want to prohibit            

            foreach (Hero hero in Hero.OneToOneConversationHero.HomeSettlement.Notables)
            {
                int nobles_relations = hero.GetRelation(Hero.OneToOneConversationHero);// smaller the relation - bigger chance to get positive result towards player
                int player_noble_relation = hero.GetRelation(Hero.MainHero); // bigger the relation - bigger chance to get positive result towards player 
                int relation_change = (player_noble_relation > nobles_relations) ? _cfg.values.relationChangeWhenLordTeachPeasant : -_cfg.values.relationChangeWhenLordTeachPeasant;
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, hero, relation_change, true);
                if (_cfg.values.enableOtherNobleTraitsChangeAfterNobleExecution)
                {

                    bool direction = MBRandom.RandomInt(-100, 100) < player_noble_relation; // bigger relation means bigger chance direction is similar to chop purpose
                    TeachHeroTraits(hero, _cfg.values.peasantRevengerExcludeTrait, chop_purpose ? direction : !direction);
                }
            }
            MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(Hero.MainHero, Hero.OneToOneConversationHero, SceneNotificationData.RelevantContextType.Map));
            KillCharacterAction.ApplyByExecution(Hero.OneToOneConversationHero, Hero.MainHero, true, false);
            leave_encounter();
        }

        private bool peasant_revenge_player_not_happy_with_peasant_start_condition()
        {
            bool start = Hero.OneToOneConversationHero != null &&
                (Hero.OneToOneConversationHero.IsHeadman || Hero.OneToOneConversationHero.IsRuralNotable) &&
                /* hero_trait_list_condition(Hero.OneToOneConversationHero, _cfg.values.peasantRevengerExcludeTrait) &&*/
                Hero.OneToOneConversationHero.Issue == null /*&& (Hero.OneToOneConversationHero.HomeSettlement.OwnerClan == Hero.MainHero.Clan ||
                Hero.OneToOneConversationHero.HomeSettlement.OwnerClan.Kingdom == Hero.MainHero.Clan.Kingdom)*/;
            if (start)
            {
                previous_can_revenge = notable_can_do_revenge(); // true if notable can do the revenge, but hero want to prohibit
            }
            return start;
        }
        #endregion

        #region ransom offer

        private void peasant_revenge_leave_lord_body_consequence()
        {
            OnLordRemainsAbandoned(Hero.MainHero);
        }

        private void peasant_revenge_player_demand_ransom_consequence(Hero criminal)
        {
            //AddKilledLordsCorpses(currentRevenge);
            //Hero criminal = currentRevenge.criminal.HeroObject;
            float ransomValue = (float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(criminal.CharacterObject, null);

            List<Hero> ransomers = GetHeroSuportersWhoCouldPayUnpaidRansom(criminal, (int)ransomValue);

            TextObject textObject = new TextObject("{=PRev0096}{RANSOMER.LINK} offer you {GOLD_AMOUNT}{GOLD_ICON} in ransom if you will give {CAPTIVE_HERO.NAME} remains.", null);
            Hero ransomer;
            if (!ransomers.IsEmpty())
            {
                ransomer = ransomers.GetRandomElementInefficiently();
                StringHelpers.SetCharacterProperties("RANSOMER", ransomer.CharacterObject, textObject, false);
                textObject.SetTextVariable("CLAN_NAME", ransomer.Clan.Name);
                textObject.SetTextVariable("GOLD_AMOUNT", ransomValue);
                StringHelpers.SetCharacterProperties("CAPTIVE_HERO", criminal.CharacterObject, textObject, false);

                InformationManager.ShowInquiry(
                    new InquiryData(
                        new TextObject("{=ho5EndaV}Decision").ToString(),
                       textObject.ToString(),
                        true,
                        true,
                        (new TextObject("{=Y94H6XnK}Accept", null)).ToString(),
                        (new TextObject("{=cOgmdp9e}Decline", null)).ToString(),
                        delegate ()
                        {
                            this.AcceptRansomRemainsOffer((int)ransomValue, Hero.MainHero, ransomer);
                        }, delegate ()
                        {
                            this.DeclineRansomOffer(Hero.MainHero, ransomer);
                        }, "", 0f, null, null, null)
                    , true, true);
            }
            else
            {
                textObject = new TextObject("{=PRev0097}Nobody want to pay for {CAPTIVE_HERO.NAME} remains");
                StringHelpers.SetCharacterProperties("CAPTIVE_HERO", criminal.CharacterObject, textObject, false);
                MBInformationManager.AddQuickInformation(textObject);
            }
        }

        private void AcceptRansomRemainsOffer(int ransomValue, Hero hero, Hero ransomer)
        {
            if (hero.PartyBelongedTo == null) return;

            ItemObject lord_body = MBObjectManager.Instance.GetObject<ItemObject>("pr_wrapped_body");
            var items = hero.PartyBelongedTo.ItemRoster;

            if (items.GetItemNumber(lord_body) < 1) return;

            ItemRosterElement item = items.Where((x) => x.EquipmentElement.Item.Name.ToString().Equals("pr_wrapped_body")).FirstOrDefault();

            OnRansomRemainsOfferAccepted(hero);
            GiveItemAction.ApplyForHeroes(hero, ransomer, item);
            GiveGoldAction.ApplyBetweenCharacters(ransomer, hero, ransomValue, false);
        }

        private void DeclineRansomOffer(Hero hero, Hero ransomer)
        {
            OnRansomRemainsOfferDeclined(hero);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(ransomer, hero,
                _cfg.values.relationChangeWhenLordDeclinedRansomOfferForCriminalLordRemains, _cfg.values.relationChangeWhenLordDeclinedRansomOfferForCriminalLordRemains != 0);
        }

        private static void AddCorpseToInventory(int count, MobileParty party)
        {
            ItemObject lord_body = MBObjectManager.Instance.GetObject<ItemObject>("pr_wrapped_body");
            var items = party.ItemRoster;
            items.AddToCounts(lord_body, count);
        }

        private void AddKilledLordsCorpses(PeasantRevengeData revenge)
        {
            int count = 0;
            if (revenge.accused_hero != null && !revenge.accused_hero.HeroObject.IsAlive) count++;
            if (revenge.criminal != null && !revenge.criminal.HeroObject.IsAlive) count++;
            if (revenge.targetHero.HeroObject.PartyBelongedTo != null)
            {
                AddCorpseToInventory(count, revenge.targetHero.HeroObject.PartyBelongedTo);
            }
        }

        #endregion

        #region trait developement
        public void OnLordPersuedeNotableToRevenge(Hero hero)
        {
            OnChangeTraits(hero, GetAffectedTraits(_cfg.values.ai.lordTraitChangeWhenLordPersuedeNotableToRevenge));
        }
        public void OnLordPersuedeNotableNotToRevenge(Hero hero)
        {
            OnChangeTraits(hero, GetAffectedTraits(_cfg.values.ai.lordTraitChangeWhenLordPersuedeNotableNotToRevenge));
        }

        public void OnLordRemainsAbandoned(Hero hero)
        {
            OnChangeTraits(hero, GetAffectedTraits(_cfg.values.ai.lordTraitChangeWhenRemainsOfLordAreAbandoned));
        }

        public void OnRansomRemainsOfferDeclined(Hero hero)
        {
            OnChangeTraits(hero, GetAffectedTraits(_cfg.values.ai.lordTraitChangeWhenRansomRemainsDeclined));
        }

        public void OnRansomRemainsOfferAccepted(Hero hero)
        {
            OnChangeTraits(hero, GetAffectedTraits(_cfg.values.ai.lordTraitChangeWhenRansomRemainsAccepted));
        }

        private Tuple<TraitObject, int>[] GetAffectedTraits(List<PeasantRevengeConfiguration.TraitAndValue> traitsAndValues)
        {
            Tuple<TraitObject, int>[] affectedTraits = new Tuple<TraitObject, int>[traitsAndValues.Count];
            for (int i = 0; i < affectedTraits.Length; i++)
            {
                affectedTraits[i] = Tuple.Create(
                    TraitObject.All.Where((x) => x.StringId.ToString() ==
                    traitsAndValues[i].trait).First(),
                    traitsAndValues[i].value);
            }

            return affectedTraits;
        }

        public void OnChangeTraits(Hero targetHero, Tuple<TraitObject, int>[] effectedTraits)
        {
            foreach (Tuple<TraitObject, int> tuple in effectedTraits)
            {
                ApplyTraitXP(tuple.Item1, tuple.Item2, ActionNotes.DefaultNote, targetHero);
            }
        }
        private void ApplyTraitXP(TraitObject trait, int xpValue, ActionNotes context, Hero referenceHero)
        {
            if (referenceHero == Hero.MainHero)
            {
                int traitLevel = referenceHero.GetTraitLevel(trait);
                Campaign.Current.PlayerTraitDeveloper.AddTraitXp(trait, xpValue);
                if (traitLevel != referenceHero.GetTraitLevel(trait))
                {
                    CampaignEventDispatcher.Instance.OnPlayerTraitChanged(trait, traitLevel);
                }
            }
            else
            {
                //???AddTraitXp(trait, xpValue); //Only player can develop trait XP by the game design.
            }
        }
        #endregion

        private void peasant_revenge_peasant_kill_by_hero(Hero executioner)
        {
            OnChangeTraits(executioner, GetAffectedTraits(_cfg.values.ai.lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest));
            MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(executioner, currentRevenge.executioner.HeroObject, SceneNotificationData.RelevantContextType.Map));
            KillCharacterAction.ApplyByExecution(currentRevenge.executioner.HeroObject, executioner, true, true);
        }

        private bool peasant_revenge_player_config_mod_start_condition()
        {
            bool start = (Hero.OneToOneConversationHero.IsHeadman || Hero.OneToOneConversationHero.IsRuralNotable) &&
                !hero_trait_list_condition(Hero.OneToOneConversationHero, _cfg.values.peasantRevengerExcludeTrait) &&
                (Hero.OneToOneConversationHero.HomeSettlement.OwnerClan == Hero.MainHero.Clan ||
                Hero.OneToOneConversationHero.HomeSettlement.OwnerClan.Kingdom == Hero.MainHero.Clan.Kingdom ||
                Hero.OneToOneConversationHero.HomeSettlement.OwnerClan.Kingdom == null ?
                !Hero.OneToOneConversationHero.HomeSettlement.OwnerClan.IsAtWarWith(Hero.MainHero.Clan.MapFaction) :
                !Hero.OneToOneConversationHero.HomeSettlement.OwnerClan.Kingdom.IsAtWarWith(Hero.MainHero.Clan.MapFaction));
            return start;
        }

        private bool peasant_revenge_ask_criminal_start_condition()
        {
            if (Hero.OneToOneConversationHero != null && currentRevenge.criminal != null &&
            Hero.OneToOneConversationHero == currentRevenge.criminal.HeroObject &&
            currentRevenge.Can_peasant_revenge_accuser_lord_start)
            {
                StringHelpers.SetCharacterProperties("CVICTIM", currentRevenge.accused_hero);
                return true;
            }
            return false;
        }

        private void peasant_revenge_criminal_blaming_consequence()
        {
            currentRevenge.Can_peasant_revenge_accuser_lord_start = true;
            CampaignMapConversation.OpenConversation(
            new ConversationCharacterData(Hero.MainHero.CharacterObject, null, false, false, false, false, false, false),
            new ConversationCharacterData(currentRevenge.criminal, null, false, false, false, false, false, false));
        }

        private void peasant_revenge_peasant_finish_start_consequence()
        {
            currentRevenge.Can_peasant_revenge_peasant_finish_start = true;
            CampaignMapConversation.OpenConversation(
            new ConversationCharacterData(Hero.MainHero.CharacterObject, null, false, false, false, false, false, false),
            new ConversationCharacterData(currentRevenge.executioner, null, false, false, false, false, false, false));
        }

        private bool peasant_revenge_peasant_finish_start_condition()
        {
            if (Hero.OneToOneConversationHero == null || currentRevenge == null) return false;

            bool retval = (Hero.OneToOneConversationHero.IsHeadman || Hero.OneToOneConversationHero.IsRuralNotable) &&
            currentRevenge.executioner != null &&
            currentRevenge.Can_peasant_revenge_peasant_finish_start &&
            currentRevenge.executioner.HeroObject == Hero.OneToOneConversationHero;

            return retval;
        }

        //This function must be tested cases, when removed revenger parties or mod itself
        private bool peasant_revenge_revenger_start_fuse_condition()
        {
            if (Hero.OneToOneConversationHero == null) return false;

            if ((Hero.OneToOneConversationHero.IsHeadman || Hero.OneToOneConversationHero.IsRuralNotable) &&
                     Hero.OneToOneConversationHero.PartyBelongedTo != null &&
                     Hero.OneToOneConversationHero.PartyBelongedTo.StringId.StartsWith(revengerPartyNameStart))
            {
                //Here we have only revenger party
                //Find, if revenger target is not player and peasant cannot start other dialogs ( status == begin )
                PeasantRevengeData revenge = revengeData.FirstOrDefault((x) =>
                x.executioner != null &&
                x.executioner.HeroObject == Hero.OneToOneConversationHero &&
                // x.targetHero != Hero.MainHero.CharacterObject &&
                x.state == PeasantRevengeData.quest_state.begin);

                bool retval = false;

                if (revenge != null) // have revenge data with peasant, who cannot start dialog (finished/not started quest)
                {
                    retval = true;
                    //setting currentRevenge because dialogue need to know whitch revenge is talking about 
                    currentRevenge = revenge;
                    create_peasant_comment_at_revenge_start(currentRevenge);
                }

                return retval;
            }
            else
            {
                return false;
            }
        }

        private void create_peasant_comment_at_revenge_start(PeasantRevengeData revenge)
        {
            string msg = "{=PRev0078}I do not have time to talk.[rf:idle_angry][ib:closed][if:idle_angry]";

            int honor = GetHeroTraitValue(revenge.executioner.HeroObject, "Honor");
            int generosity = GetHeroTraitValue(revenge.executioner.HeroObject, "Generosity");

            if (honor > 0)
            {
                msg = "{=PRev0114}Nobody can stop my revenge on {CRIMINAL.LINK}![rf:idle_angry][if:convo_furious][ib:angry]";
            }
            else if (honor < 0)
            {
                msg = "{=PRev0116}{CRIMINAL.LINK} will die![if:convo_furious][ib:angry]";
            }

            if (generosity < 0)
            {
                msg = "{=PRev0113}{CRIMINAL.LINK} will pay {GOLD_ICON} or die![rf:idle_angry][ib:angry]";
            }

            TextObject textObject = new TextObject(msg, null);
            StringHelpers.SetCharacterProperties("CRIMINAL", revenge.criminal, textObject, false);
            MBTextManager.SetTextVariable("COMMENT_REVENGE_START", textObject);
        }

        private bool peasant_revenge_revenger_start_ended_fuse_condition()
        {
            if (Hero.OneToOneConversationHero == null) return false;

            if ((Hero.OneToOneConversationHero.IsHeadman || Hero.OneToOneConversationHero.IsRuralNotable) &&
                     Hero.OneToOneConversationHero.PartyBelongedTo != null &&
                     Hero.OneToOneConversationHero.PartyBelongedTo.StringId.StartsWith(revengerPartyNameStart))
            {
                //Here we have only revenger party
                //Find, if revenger target is not player and peasant cannot start other dialogs ( status == begin )
                PeasantRevengeData revenge = revengeData.FirstOrDefault((x) =>
                x.executioner != null &&
                x.executioner.HeroObject == Hero.OneToOneConversationHero &&
                // x.targetHero != Hero.MainHero.CharacterObject &&
                x.state > PeasantRevengeData.quest_state.start);

                bool retval = false;

                if (revenge != null) // have revenge data with peasant, who cannot start dialog (finished/not started quest)
                {
                    retval = true;
                    //setting currentRevenge because dialogue need to know whitch revenge is talking about 
                    currentRevenge = revenge;
                    create_peasant_comment_at_revenge_end(currentRevenge);
                }

                return retval;
            }
            else
            {
                return false;
            }
        }

        private void create_peasant_comment_at_revenge_end(PeasantRevengeData revenge)
        {
            string msg = "{=PRev0078}I do not have time to talk.[rf:idle_angry][ib:closed][if:idle_angry]";

            if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed) &&
            currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed))
            {
                msg = "{=PRev0111}{CRIMINAL.NAME} and {CVICTIM.LINK} are dead! My revenge is completed![if:happy]";
            }
            else
            {
                if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed))
                {
                    msg = "{=PRev0112}I finished the revenge on {CVICTIM.LINK}![if:happy]";
                }
                else if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
                {
                    msg = "{=PRev0112}I finished the revenge on  {CRIMINAL.LINK}![if:happy]";
                }
            }

            TextObject textObject = new TextObject(msg, null);
            if (currentRevenge.accused_hero != null)
            {
                StringHelpers.SetCharacterProperties("CVICTIM", currentRevenge.accused_hero, textObject, false);
            }
            StringHelpers.SetCharacterProperties("CRIMINAL", revenge.criminal, textObject, false);
            MBTextManager.SetTextVariable("COMMENT_REVENGE_END", textObject);
        }

        private bool peasant_revenge_enable_intimidation_clickable_condition(out TextObject textObject)
        {
            if (Hero.MainHero.PartyBelongedTo.Party.TotalStrength > currentRevenge.xParty.Party.TotalStrength * _cfg.values.peasantRevengerIntimidationPowerScale)
            {
                textObject = new TextObject("");
            }
            else
            {
                textObject = new TextObject("{=PRev0115}Your party is too small");
                return false;
            }

            return true;
        }

        private void TeachHeroTraits(Hero hero, string traits, bool direction, params Hero[] teacher)
        {
            if (string.IsNullOrEmpty(traits)) return;

            List<string> traits_con_pool = traits.Split('|').ToList();
            string[] traits_con = traits_con_pool.ToArray();

            foreach (string trait_or in traits_con)
            {
                string[] trait_or_con = trait_or.Split('&');

                foreach (string trait in trait_or_con)
                {
                    string[] a = trait.Split(' ');
                    int value = GetHeroTraitValue(hero, a[0]);

                    if (!teacher.IsEmpty())
                    {
                        int target = GetHeroTraitValue(teacher.First(), a[0]);

                        if (a[1].Contains(">"))
                        {
                            value = value > target ? direction ? value : target : direction ? target : value;
                        }
                        else if (a[1].Contains("<"))
                        {
                            value = value < target ? direction ? value : target : direction ? target : value;
                        }
                        else if (a[1].Contains("=="))
                        {
                            value = value == target ? value : value > target ? direction ? value : target : direction ? target : value;
                        }
                    }
                    else
                    {
                        int target = int.Parse(a[2]);

                        if (a[1].Contains(">"))
                        {
                            value = direction ? target + 1 : target - 1;
                        }
                        else if (a[1].Contains("<"))
                        {
                            value = direction ? target - 1 : target + 1;
                        }
                        else if (a[1].Contains("=="))
                        {
                            value = target;
                        }
                    }

                    SetHeroTraitValue(hero, $"{a[0]}", value);
                }
            }
        }

        private void peasant_revenge_criminal_has_suporters_consequence()
        {
            List<Hero> savers = GetHeroSuportersWhoCouldPayUnpaidRansom(currentRevenge.criminal.HeroObject, currentRevenge.reparation);
            if (!savers.IsEmpty())
            {
                Hero saver = savers.GetRandomElementInefficiently();
                GiveGoldAction.ApplyBetweenCharacters(saver, currentRevenge.executioner.HeroObject, (int)currentRevenge.reparation, false);
                string LogMessage = "{=PRev0040}{PARTYOWNER.NAME} did not executed {PRISONER.NAME}, because {SAVER.NAME} paid {REPARATION}{GOLD_ICON}.";
                TextObject textObject = new TaleWorlds.Localization.TextObject(LogMessage, null);
                StringHelpers.SetCharacterProperties("SAVER", saver.CharacterObject, textObject, false);
                StringHelpers.SetCharacterProperties("PRISONER", currentRevenge.criminal, textObject, false);
                StringHelpers.SetCharacterProperties("PARTYOWNER", currentRevenge.party.Owner.CharacterObject, textObject, false);
                textObject.SetTextVariable("REPARATION", (float)currentRevenge.reparation);
                InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Color.ConvertStringToColor(_cfg.values.logColorForClan)));
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(saver, Hero.MainHero, _cfg.values.relationLordAndCriminalChangeWhenLordSavedTheCriminal, _cfg.values.relationLordAndCriminalChangeWhenLordSavedTheCriminal != 0);
            }
        }

        private bool peasant_revenge_criminal_has_suporters_clickable_condition(out TextObject textObject)
        {
            List<Hero> saver = GetHeroSuportersWhoCouldPayUnpaidRansom(currentRevenge.criminal.HeroObject, currentRevenge.reparation);
            bool start = !saver.IsEmpty();
            if (saver.Count() == 1)
            {
                textObject = new TextObject("{=PRev0066}{SAVER.NAME} will support you.");
                StringHelpers.SetCharacterProperties("SAVER", saver.First().CharacterObject, textObject, false);
            }
            else
            {
                textObject = new TextObject("{=PRev0067}{SUPPORTERCOUNT} heroes can support you.");
                MBTextManager.SetTextVariable("SUPPORTERCOUNT", (float)saver.Count());
            }

            return start;
        }

        private bool peasant_revenge_enable_party_clickable_condition(out TextObject textObject)
        {
            if (_cfg.values.enableRevengerMobileParty)
            {
                textObject = new TextObject("{=PRev0088}Disable notable peasant mobile party");
            }
            else
            {
                textObject = new TextObject("{=PRev0089}Enable notable peasant mobile party");
            }

            return true;
        }
        private bool peasant_revenge_enable_neutral_village_attack_clickable_condition(out TextObject textObject)
        {
            if (_cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu)
            {
                textObject = new TextObject("{=PRev0090}Disable the option to defend the village against neutral mobile party");
            }
            else
            {
                textObject = new TextObject("{=PRev0091}Enable the option to defend the village against neutral mobile party");
            }

            return true;
        }





        private bool peasant_revenge_lord_start_reparation_condition()
        {
            MBTextManager.SetTextVariable("REPARATION", currentRevenge.reparation);
            return true;
        }

        private void peasant_revenge_end_revenge_consequence()
        {
            currentRevenge.Stop();
            leave_encounter();
        }

        private bool WillLordDemandSupport(Hero receiverHero)
        {
            bool rezult =
                receiverHero.Gold < _cfg.values.lordWillDemandRansomMoneyIfHasLessGoldThan ||
                hero_trait_list_condition(receiverHero, _cfg.values.lordWillAskRansomMoneyIfHasTraits);
            return rezult;
        }

        private bool WillLordSupportHeroClaim(Hero suporterHero, Hero receiverHero)
        {
            bool rezult = hero_trait_list_condition(suporterHero, _cfg.values.lordWillOfferRansomMoneyIfHasTraits, receiverHero);

            if (!rezult)
            {
                rezult = MBRandom.RandomInt(0, _cfg.values.lordWillOfferRansomMoneyWithProbabilityIfTraitFails) <= _cfg.values.lordWillOfferRansomMoneyWithProbabilityIfTraitFails;
            }

            return rezult;
        }

        private bool peasant_revenge_party_need_compensation_for_killed_pow_condition()
        {
            currentRevenge.Can_peasant_revenge_support_lord_start = WillLordDemandSupport(currentRevenge.party.LeaderHero);
            return currentRevenge.Can_peasant_revenge_support_lord_start;
        }

        private void peasant_revenge_party_need_compensation_for_killed_pow_consequence()
        {
            CharacterObject speaker = currentRevenge.nobleParty.MemberRoster.GetCharacterAtIndex(currentRevenge.nobleParty.MemberRoster.Count - 1);
            CampaignMapConversation.OpenConversation(
            new ConversationCharacterData(Hero.MainHero.CharacterObject, null, false, false, false, false, false, false),
            new ConversationCharacterData(speaker, currentRevenge.nobleParty, false, false, false, false, false, false));
        }

        private bool peasant_revenge_party_need_compensation_gift_condition()
        {
            MBTextManager.SetTextVariable("RANSOM_COMPENSATION", (float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(currentRevenge.criminal, null));
            StringHelpers.SetCharacterProperties("GIFT_RECEIVER", currentRevenge.party.LeaderHero.CharacterObject, null, false);
            return true;
        }

        private bool peasant_revenge_party_get_compensation_gift_condition()
        {
            StringHelpers.SetCharacterProperties("GIFT_RECEIVER", currentRevenge.party.LeaderHero.CharacterObject, null, false);
            return true;
        }

        private void peasant_revenge_party_need_compensation_not_payed_party_killed_consequence()
        {
            currentRevenge.Stop();

            CharacterObject speaker = currentRevenge.nobleParty.MemberRoster.GetCharacterAtIndex(currentRevenge.nobleParty.MemberRoster.Count - 1);
            currentRevenge.nobleParty.MemberRoster.RemoveTroop(speaker, 1);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.party.LeaderHero, _cfg.values.relationChangeAfterLordPartyGotNoReward, _cfg.values.relationChangeAfterLordPartyGotNoReward != 0);
            currentRevenge.Can_peasant_revenge_support_lord_start = false;
            leave_encounter();
        }


        private void leave_encounter()
        {
            if (PlayerEncounter.Current == null) return;
            PlayerEncounter.LeaveEncounter = true;
            if (currentRevenge.xParty != null) currentRevenge.xParty.Ai.SetMoveModeHold();
        }

        private void peasant_revenge_party_need_compensation_not_payed_consequence()
        {
            currentRevenge.Stop();

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.party.LeaderHero, _cfg.values.relationChangeAfterLordPartyGotNoReward, _cfg.values.relationChangeAfterLordPartyGotNoReward != 0);
            currentRevenge.Can_peasant_revenge_support_lord_start = false;
            leave_encounter();
        }

        private void peasant_revenge_party_need_compensation_payed_consequence()
        {
            currentRevenge.Stop();

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.party.LeaderHero, _cfg.values.relationChangeAfterLordPartyGotPaid, _cfg.values.relationChangeAfterLordPartyGotPaid != 0);
            currentRevenge.Can_peasant_revenge_support_lord_start = false;
            leave_encounter();
        }

        private bool peasant_revenge_party_need_compensation_condition()
        {
            return currentRevenge.Can_peasant_revenge_support_lord_start;
        }
        public bool InitializeGiftBarterableBarterContext(Barterable barterable, BarterData args, object obj)
        {
            return barterable.GetType() == typeof(GiftBarterable) && barterable.OriginalOwner == Hero.OneToOneConversationHero;
        }

        private void peasant_revenge_party_need_compensation_player_barter_consequence()
        {
            List<Barterable> barterables = new List<Barterable>();
            float reansomValue = (float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(currentRevenge.criminal, null);
            barterables.Add(new GiftBarterable(currentRevenge.party.LeaderHero, currentRevenge.party, null, Hero.MainHero, (int)reansomValue));
            BarterManager instance = BarterManager.Instance;
            instance.StartBarterOffer(
                Hero.MainHero,
                currentRevenge.party.LeaderHero,
                PartyBase.MainParty,
                currentRevenge.party ?? null, null,
                new BarterManager.BarterContextInitializer(InitializeGiftBarterableBarterContext), 0, false, barterables);
        }

        private void peasant_revenge_peasant_messenger_killed_consequence()
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.party.LeaderHero, _cfg.values.relationChangeWhenLordKilledMessenger, _cfg.values.relationChangeWhenLordKilledMessenger != 0);
            MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(Hero.MainHero, currentRevenge.executioner.HeroObject, SceneNotificationData.RelevantContextType.Map));
            KillCharacterAction.ApplyByExecution(currentRevenge.executioner.HeroObject, Hero.MainHero, true, true);
        }

        private bool peasant_revenge_peasant_messenger_killed_condition()
        {
            if (_cfg.values.allowLordToKillMessenger == false) return false;
            if (Hero.MainHero.IsPrisoner) return false; // cannot kill somebody if player is prisoner
            if (currentRevenge.party.LeaderHero == null) return false;
            StringHelpers.SetCharacterProperties("HERO", currentRevenge.party.LeaderHero.CharacterObject, null, false);
            return true;
        }

        private bool peasant_revenge_peasant_messenger_fill_hero_condition()
        {
            if (currentRevenge.party.LeaderHero == null) return false;
            StringHelpers.SetCharacterProperties("HERO", currentRevenge.party.LeaderHero.CharacterObject, null, false);
            return true;
        }

        private bool player_has_enougth_gold_condition()
        {
            MBTextManager.SetTextVariable("REPARATION", currentRevenge.reparation);
            return Hero.MainHero.Gold >= currentRevenge.reparation;
        }

        private void peasant_revenge_peasant_messenger_not_kill_hero_consequence()
        {
            currentRevenge.Stop();

            if (currentRevenge.accused_hero != null && currentRevenge.accused_hero != currentRevenge.criminal)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.accused_hero.HeroObject, _cfg.values.relationChangeWhenPlayerSavedTheCriminal, _cfg.values.relationChangeWhenPlayerSavedTheCriminal != 0);
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.criminal.HeroObject, _cfg.values.relationLordAndCriminalChangeWhenLordSavedTheCriminal, _cfg.values.relationLordAndCriminalChangeWhenLordSavedTheCriminal != 0);
            }
            else
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.criminal.HeroObject, _cfg.values.relationChangeWhenPlayerSavedTheCriminal, _cfg.values.relationChangeWhenPlayerSavedTheCriminal != 0);
            }

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentRevenge.executioner.HeroObject, currentRevenge.party.LeaderHero, _cfg.values.relationChangeWhenLordRefusedToPayReparations, false);

            log($"{currentRevenge.party.LeaderHero.Name} captured and {currentRevenge.executioner.Name} did not executed {currentRevenge.criminal.Name}");
        }

        private void ExecuteHero(Hero victim)
        {
            if (_cfg.values.allowPeasantToKillLord)
            {
                MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(currentRevenge.executioner.HeroObject, victim, SceneNotificationData.RelevantContextType.Map)); // do not show because prisoner is in other party
                KillCharacterAction.ApplyByExecution(victim, currentRevenge.executioner.HeroObject, true, true);
                log($"{currentRevenge.party.LeaderHero.Name} captured and {currentRevenge.executioner.Name} executed {victim.Name}, because lack {currentRevenge.reparation - victim.Gold} gold");
            }
            else
            {
                MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(currentRevenge.party.LeaderHero, victim, SceneNotificationData.RelevantContextType.Map)); // do not show because prisoner is in other party
                KillCharacterAction.ApplyByExecution(victim, currentRevenge.party.LeaderHero, true, true);
                log($"{currentRevenge.party.LeaderHero.Name} captured and {currentRevenge.party.LeaderHero.Name} executed {victim.Name}, because lack {currentRevenge.reparation - victim.Gold} gold");
            }
        }

        private void peasant_revenge_peasant_messenger_kill_victim_consequence()
        {
            Hero victim = currentRevenge.accused_hero == null ? currentRevenge.criminal.HeroObject : currentRevenge.accused_hero.HeroObject;

            if (currentRevenge.accused_hero != null && currentRevenge.accused_hero != currentRevenge.criminal)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentRevenge.targetHero.HeroObject, currentRevenge.criminal.HeroObject, _cfg.values.relationChangeWhenLordExecutedTheCriminal, _cfg.values.relationChangeWhenLordExecutedTheCriminal != 0);
            }

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentRevenge.targetHero.HeroObject, currentRevenge.executioner.HeroObject, _cfg.values.relationChangeWhenLordExecutedTheCriminal, _cfg.values.relationChangeWhenLordExecutedTheCriminal != 0);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentRevenge.executioner.HeroObject, currentRevenge.party.LeaderHero, _cfg.values.relationChangeWhenLordExecutedTheCriminal, _cfg.values.relationChangeWhenLordExecutedTheCriminal != 0);
            ExecuteHero(victim);
        }

        private void peasant_revenge_peasant_kill_the_criminal()
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentRevenge.targetHero.HeroObject, currentRevenge.executioner.HeroObject, _cfg.values.relationChangeWhenLordExecutedTheCriminal, _cfg.values.relationChangeWhenLordExecutedTheCriminal != 0);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentRevenge.executioner.HeroObject, currentRevenge.party.LeaderHero, _cfg.values.relationChangeWhenLordExecutedTheCriminal, _cfg.values.relationChangeWhenLordExecutedTheCriminal != 0);
            ExecuteHero(currentRevenge.criminal.HeroObject);
        }

        private void peasant_revenge_peasant_messenger_kill_both_consequence()
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentRevenge.executioner.HeroObject, currentRevenge.party.LeaderHero, _cfg.values.relationChangeWhenLordExecutedTheCriminal, _cfg.values.relationChangeWhenLordExecutedTheCriminal != 0);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentRevenge.executioner.HeroObject, currentRevenge.targetHero.HeroObject, _cfg.values.relationChangeWhenLordExecutedTheCriminal, _cfg.values.relationChangeWhenLordExecutedTheCriminal != 0);
            ExecuteHero(currentRevenge.criminal.HeroObject);
            ExecuteHero(currentRevenge.accused_hero.HeroObject);
        }

        private void peasant_revenge_hero_cannot_make_decision_consequence(Hero hero)
        {
            currentRevenge.Stop();

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, currentRevenge.executioner.HeroObject,
             _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge, _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge != 0 && hero == Hero.MainHero);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, currentRevenge.criminal.HeroObject,
            _cfg.values.relationChangeWhenPlayerSavedTheCriminal, _cfg.values.relationChangeWhenPlayerSavedTheCriminal != 0 && hero == Hero.MainHero);
        }

        private void criminal_has_to_pay_in_gold_consequence()
        {
            currentRevenge.Stop();

            GiveGoldAction.ApplyBetweenCharacters(currentRevenge.criminal.HeroObject, currentRevenge.executioner.HeroObject, (int)currentRevenge.reparation, true);
            TextObject textObject = new TaleWorlds.Localization.TextObject("{=PRev0046}{HERO.NAME} paid {REPARATION}{GOLD_ICON} to {EXECUTIONER.NAME}.", null);
            StringHelpers.SetCharacterProperties("HERO", currentRevenge.criminal, textObject, false);
            StringHelpers.SetCharacterProperties("EXECUTIONER", currentRevenge.executioner, textObject, false);
            textObject.SetTextVariable("REPARATION", (float)currentRevenge.reparation);
            MBInformationManager.AddQuickInformation(textObject, 100, null, "");

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.executioner.HeroObject, _cfg.values.relationChangeAfterReparationsReceived, _cfg.values.relationChangeAfterReparationsReceived != 0);
        }

        private void player_pay_messenger_in_gold_consequence()
        {
            currentRevenge.Stop();

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, currentRevenge.executioner.HeroObject, (int)currentRevenge.reparation, true);
            TextObject textObject = new TextObject("{=PRev0046}{HERO.NAME} paid {REPARATION}{GOLD_ICON} to {EXECUTIONER.NAME}.", null);
            StringHelpers.SetCharacterProperties("HERO", Hero.MainHero.CharacterObject, textObject, false);
            StringHelpers.SetCharacterProperties("EXECUTIONER", currentRevenge.executioner, textObject, false);
            textObject.SetTextVariable("REPARATION", (float)currentRevenge.reparation);
            MBInformationManager.AddQuickInformation(textObject, 100, null, "");

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.executioner.HeroObject, _cfg.values.relationChangeAfterReparationsReceived, _cfg.values.relationChangeAfterReparationsReceived != 0);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentRevenge.criminal.HeroObject, _cfg.values.relationChangeWhenPlayerSavedTheCriminal, _cfg.values.relationChangeWhenPlayerSavedTheCriminal != 0);
        }

        private bool criminal_has_enougth_gold_condition()
        {
            MBTextManager.SetTextVariable("REPARATION", currentRevenge.reparation);
            return currentRevenge.criminal.HeroObject.Gold >= currentRevenge.reparation;
        }

        public bool InitializeReparationsBarterableBarterContext(Barterable barterable, BarterData args, object obj)
        {
            return barterable.GetType() == typeof(ReparationsBarterable) && barterable.OriginalOwner == Hero.OneToOneConversationHero;
        }

        private void peasant_revenge_player_barter_consequence()
        {
            currentRevenge.Stop();

            Hero heroBeingProposedTo = Hero.OneToOneConversationHero;
            List<Barterable> barterables = new List<Barterable>();
            MobileParty partyBelongedTo = Hero.OneToOneConversationHero.PartyBelongedTo;

            barterables.Add(new ReparationsBarterable(Hero.OneToOneConversationHero, PartyBase.MainParty, null, Hero.MainHero, currentRevenge.reparation));
            BarterManager instance = BarterManager.Instance;
            instance.StartBarterOffer(
                Hero.MainHero,
                Hero.OneToOneConversationHero,
                PartyBase.MainParty,
                partyBelongedTo?.Party, null,
                new BarterManager.BarterContextInitializer(InitializeReparationsBarterableBarterContext), 0, false, barterables);
        }

        private bool barter_successful_condition()
        {
            return Campaign.Current.BarterManager.LastBarterIsAccepted;
        }

        private void peasant_revenge_player_payed_consecuence()
        {
            currentRevenge.Stop();
            GiveGoldAction.ApplyBetweenCharacters(Hero.OneToOneConversationHero, currentRevenge.executioner.HeroObject, (int)currentRevenge.reparation, true); //because party leader got all gold 
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, currentRevenge.executioner.HeroObject, _cfg.values.relationChangeAfterReparationsReceived, false);
        }

        private void peasant_revenge_cannot_pay_consequence()
        {
            currentRevenge.Stop();

            if (!kill_main_hero())
            {
                ChangeRelationAction.ApplyPlayerRelation(currentRevenge.executioner.HeroObject, _cfg.values.relationChangeWhenCriminalRefusedToPayReparations, true, true);
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, currentRevenge.executioner.HeroObject, _cfg.values.relationChangeWhenCannotPayReparations, false);
            }
        }

        private bool will_party_leader_kill_the_criminal()
        {
            bool party_relatives_with_criminal_condition = (currentRevenge.party.Owner.Children.Contains(currentRevenge.criminal.HeroObject) ||
                currentRevenge.criminal.HeroObject.Children.Contains(currentRevenge.party.Owner)) &&
                                                          CheckConditions(currentRevenge.party.Owner, currentRevenge.criminal.HeroObject,
                                                          _cfg.values.ai.lordIfRelativesWillHelpTheCriminal);
            bool party_help_criminal_con = CheckConditions(currentRevenge.party.Owner,
                currentRevenge.executioner.HeroObject, _cfg.values.ai.lordWillAffordToHelpTheCriminalEnemy);
            bool party_friend_to_criminal_con = currentRevenge.party.Owner.IsFriend(currentRevenge.criminal.HeroObject);
            bool party_overide_con = CheckConditions(currentRevenge.party.Owner,
                currentRevenge.executioner.HeroObject, _cfg.values.ai.partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet) ||
                currentRevenge.party.Owner.IsFriend(currentRevenge.executioner.HeroObject);
            bool party_let_revenge_con = (!party_help_criminal_con && !party_friend_to_criminal_con && !party_relatives_with_criminal_condition) || party_overide_con;

            return party_let_revenge_con;
        }

        private bool kill_main_hero()
        {
            bool canMainHeroDie = Hero.MainHero.CanDie(KillCharacterAction.KillCharacterActionDetail.Executed) && will_party_leader_kill_the_criminal();
            if (canMainHeroDie)
            {
                if (_cfg.values.allowPeasantToKillLord)
                {
                    KillCharacterAction.ApplyByExecution(Hero.MainHero, currentRevenge.executioner.HeroObject, false);
                }
                else
                {
                    KillCharacterAction.ApplyByExecution(Hero.MainHero, currentRevenge.party.Owner, false);
                }
                return true;
            }
            return false;
        }

        private void peasant_revenge_peasant_kill_victim_consequence_lied()
        {
            currentRevenge.Stop();
            ChangeRelationAction.ApplyPlayerRelation(currentRevenge.executioner.HeroObject, _cfg.values.relationChangeWhenLordExecutedTheCriminal, true, true);
            ExecuteHero(currentRevenge.accused_hero.HeroObject);
        }
        private void peasant_revenge_peasant_kill_both_consequence_lied()
        {
            currentRevenge.Stop();
            ExecuteHero(currentRevenge.accused_hero.HeroObject);
            kill_main_hero();
        }
        private void peasant_revenge_peasant_not_kill_hero_consequence()
        {
            currentRevenge.Stop();

            if (currentRevenge.executioner.HeroObject.HomeSettlement.OwnerClan.Kingdom == Hero.MainHero.Clan.Kingdom)
            {
                ChangeRelationAction.ApplyPlayerRelation(currentRevenge.executioner.HeroObject, _cfg.values.relationChangeWhenLordRefusedToPayReparations, true, true);
            }
            else
            {
                ChangeRelationAction.ApplyPlayerRelation(currentRevenge.executioner.HeroObject, _cfg.values.relationChangeWhenCannotPayReparations, true, true);
            }
        }

        private bool peasant_revenge_peasant_start_condition()
        {
            if (Hero.OneToOneConversationHero == null) return false;

            PeasantRevengeData revenge = revengeData.Where((x) =>
            x.executioner != null &&
            x.executioner.HeroObject == Hero.OneToOneConversationHero &&
            x.Can_peasant_revenge_peasant_start).FirstOrDefault();

            if (revenge == null) return false;

            currentRevenge = revenge;
            currentRevenge.accused_hero = getAllyPrisonerTheEscapeGoat(currentRevenge.criminal.HeroObject);
            StringHelpers.SetCharacterProperties("CRIMINAL", currentRevenge.criminal, null, false);

            return true;
        }

        private bool have_accused_hero()
        {
            return currentRevenge.accused_hero != null;
        }

        private bool peasant_revenge_peasant_messenger_start_condition()
        {
            if (Hero.OneToOneConversationHero == null) return false;

            PeasantRevengeData revenge = revengeData.Where((x) =>
            x.executioner != null &&
            x.executioner.HeroObject == Hero.OneToOneConversationHero &&
            x.Can_peasant_revenge_messenger_peasant_start).FirstOrDefault();

            if (revenge == null) return false;

            currentRevenge = revenge;
            TextObject text;

            if (have_accused_hero())
            {
                text = new TextObject("{=PRev0068}{PARTYLEADER.LINK} caught {CRIMINAL.LINK} looting our village, and {CVICTIM.LINK} has been accused of planning all of it. We demand justice![ib:aggressive][if:convo_furious]");
                StringHelpers.SetCharacterProperties("CVICTIM", currentRevenge.accused_hero, text, false);
            }
            else
            {
                text = new TextObject("{=PRev0021}{PARTYLEADER.LINK} caught {CRIMINAL.LINK} looting our village. We demand criminal's head on spike, because bastard must pay for the crime! What will you say?[ib:aggressive][if:convo_furious]");
            }

            StringHelpers.SetCharacterProperties("CRIMINAL", currentRevenge.criminal, text, false);
            StringHelpers.SetCharacterProperties("PARTY", currentRevenge.party.LeaderHero.CharacterObject, text, false);
            StringHelpers.SetCharacterProperties("PARTYLEADER", currentRevenge.party.LeaderHero.CharacterObject, text, false);
            MBTextManager.SetTextVariable("PEASANTDEMANDS", text);

            return true;
        }

        private bool peasant_revenge_lord_start_condition()
        {
            if (Hero.OneToOneConversationHero == null) return false;

            PeasantRevengeData revenge = revengeData.Where((x) =>
            x.party != null &&
            x.party.LeaderHero != null &&
            x.party.LeaderHero == Hero.OneToOneConversationHero &&
            x.Can_peasant_revenge_lord_start).FirstOrDefault();

            if (revenge == null) return false;

            currentRevenge = revenge;

            if (currentRevenge.village.Settlement.OwnerClan == currentRevenge.criminal.HeroObject.Clan) return false;

            return true;
        }

        private bool peasant_revenge_lord_start_condition_betray()
        {
            if (Hero.OneToOneConversationHero == null) return false;

            PeasantRevengeData revenge = revengeData.Where((x) =>
            x.party != null &&
            x.party.LeaderHero != null &&
            x.party.LeaderHero == Hero.OneToOneConversationHero &&
            x.Can_peasant_revenge_lord_start).FirstOrDefault();

            if (revenge == null) return false;

            currentRevenge = revenge;

            if (currentRevenge.village.Settlement.OwnerClan != currentRevenge.criminal.HeroObject.Clan)
            {
                return false;
            }
            else
            {
                StringHelpers.SetCharacterProperties("PEASANTREVENGER", currentRevenge.executioner, null, false);
            }

            return true;
        }

        private CharacterObject getAllyPrisonerTheEscapeGoat(Hero hero)
        {
            if (hero == null
                || hero.PartyBelongedToAsPrisoner == null
                || hero.PartyBelongedToAsPrisoner.PrisonerHeroes == null
                || hero.PartyBelongedToAsPrisoner.PrisonerHeroes.IsEmpty())
                return null;
            var prisoners = hero.PartyBelongedToAsPrisoner.PrisonerHeroes.Where((x) =>
              x != null &&
              x.HeroObject != null &&
              x.HeroObject.Clan != null &&
              !x.HeroObject.Clan.IsAtWarWith(hero.Clan) &&
              x.HeroObject != hero &&
              CheckConditions(hero, x.HeroObject, _cfg.values.ai.criminalWillBlameOtherLordForTheCrime) &&
              (x.HeroObject.Clan == hero.Clan || x.HeroObject.Clan.Kingdom == hero.Clan.Kingdom));

            if (prisoners != null && !prisoners.IsEmpty())
            {
                return prisoners.First();
            }
            return null;
        }

        private bool peasant_revenge_lord_start_condition_peasant_rat_betray()
        {
            if (!currentRevenge.Can_peasant_revenge_lord_start) return false;
            if (currentRevenge.village.Settlement.OwnerClan != currentRevenge.criminal.HeroObject.Clan)
            {
                return false;
            }
            else
            {
                StringHelpers.SetCharacterProperties("PEASANTREVENGER", currentRevenge.executioner, null, false);
            }

            return currentRevenge.party.LeaderHero == Hero.OneToOneConversationHero;
        }

        private bool peasant_revenge_lord_start_condition_lie()
        {
            if (!currentRevenge.Can_peasant_revenge_lord_start) return false;

            if (currentRevenge.party.LeaderHero == Hero.OneToOneConversationHero)
            {
                var victims = currentRevenge.party.PrisonerHeroes.Where((x) =>
               !x.HeroObject.Clan.IsAtWarWith(Hero.MainHero.Clan) && x.HeroObject != Hero.MainHero &&
               (x.HeroObject.Clan == Hero.MainHero.Clan || x.HeroObject.Clan.Kingdom == Hero.MainHero.Clan.Kingdom));

                if (!victims.IsEmpty())
                {
                    currentRevenge.accused_hero = victims.First();
                    StringHelpers.SetCharacterProperties("COMPANION", currentRevenge.accused_hero, null, false);
                }
                else
                {
                    return false;
                }
            }

            return currentRevenge.party.LeaderHero == Hero.OneToOneConversationHero;
        }

        private void log(string text)
        {
            if (!string.IsNullOrEmpty(_cfg.values.log_file_name))
            {
                File.AppendAllText(_cfg.values.log_file_name, $"{CampaignTime.Now}: {text}\r");
            }
        }


    }
}
