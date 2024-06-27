using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                cancelled
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

        private bool IsModuleVersionOlder(ApplicationVersion module_version, ApplicationVersion compare)
        {
            bool is_older = true;

            if(module_version.Major>compare.Major)
            {
                is_older=false;
            }
            else if(module_version.Major==compare.Major)
            {
                if(module_version.Minor>compare.Minor)
                {
                    is_older=false;
                }
                else if(module_version.Minor==compare.Minor)
                {
                    if(module_version.Revision>=compare.Revision)
                    {
                        is_older=false;
                    }
                }
            }
            return is_older;
        }

        public PeasantRevengeConfiguration CheckModules(PeasantRevengeConfiguration cfg_source)
        {
            string[] moduleNames = Utilities.GetModulesNames();

            foreach (string modulesId in moduleNames)
            {
                if (modulesId.Contains("Bannerlord.Diplomacy")) // Diplomacy mod patch
                {
                    bool need_patch = IsModuleVersionOlder(
                         TaleWorlds.ModuleManager.ModuleHelper.GetModuleInfo(modulesId).Version,
                         new ApplicationVersion(ApplicationVersionType.Release,1,2,10,0));

                    if(need_patch)
                    {
                        cfg_source.allowLordToKillMessenger=false;
                        cfg_source.allowPeasantToKillLord=false;                        
                    }
                    break; // because there is no more module patches it should end the configuration
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
                "{=PRev0087}Declare war on {KINGDOM} and help {DEFENDER}.",
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
                    TextObject menuText = new TextObject("{=PRev0086}You decide to...");
                    MBTextManager.SetTextVariable("ENCOUNTER_TEXT", menuText, true);
                }
                return;
            }
        }

        #endregion
       
        private void StopRevengeForNotableIfAny(Hero revenger)
        {
            if(revenger!=null)
            {
                IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) =>
                x.executioner==revenger.CharacterObject
               );

                if(currentData!=null&&!currentData.IsEmpty())
                {
                    foreach(PeasantRevengeData revenge in currentData)
                    {
                        revenge.Stop();
                    }
                }
            }
        }
        
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
            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) =>
            x.xParty != null &&
            ((x.xParty.Party?.Id.ToString().Equals(party?.Id.ToString())) ?? false));

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
            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) =>
            (x.party?.Id.ToString().Equals(party?.Id.ToString())) ?? false);

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
            x.criminal == victim.CharacterObject ||
             x.targetHero == victim.CharacterObject ||
             x.executioner == victim.CharacterObject
            );

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

            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) =>
            x.criminal == village.Settlement.LastAttackerParty.Party.LeaderHero.CharacterObject &&
            x.village == village);

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
            IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) =>
            x.criminal == prisoner.CharacterObject &&
            x.state == PeasantRevengeData.quest_state.none); // in case of other state it should not overwrite

            if (currentData != null && !currentData.IsEmpty())
            {
                foreach (PeasantRevengeData revenge in currentData)
                {
                    if (revenge.criminal.HeroObject == prisoner && revenge.executioner == null)
                    {
                        CharacterObject executioner = GetRevengeNotable(revenge.village.Settlement); // same revenger can be added to many revenges

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

            currentData = revengeData.Where((x) => (x.targetHero == prisoner.CharacterObject) || (x.executioner == prisoner.CharacterObject));

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
            x.criminal == criminal.CharacterObject);

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
            if(revengerPartiesCleanUp)
            {
                revengerPartiesCleanUp = !FindRevengesForRevengeParties();
            }

            for (int i = 0; i < revengeData.Count; i++) //Do not remove revengeData elsewhere (only should add in other threads or events)
            {
                if (revengeData[i].state == PeasantRevengeData.quest_state.ready)
                {
                    if (revengeData[i].startTime.IsPast)
                    {
                        if(_cfg.values.enableRevengerMobileParty)
                        {
                            if(revengeData [i].party!=null && revengeData [i].party.MobileParty!=null)
                            {
                                // making sure not to create parties with the same executioner
                                bool with_party = false;
                                for(int k = 0;k<revengeData.Count;k++)
                                {
                                    if(i!=k)
                                    {
                                        if(revengeData [k].executioner!=null)
                                        {
                                            if(revengeData [k].executioner == revengeData [i].executioner)
                                            {
                                                with_party = revengeData [k].xParty != null;
                                                if(with_party)
                                                    break;
                                            }
                                        }
                                    }
                                }

                                if(with_party==false)
                                {
                                    revengeData [i].xParty=CreateNotableParty(revengeData [i]);
                                    revengeData [i].xParty.Ai.SetMoveEscortParty(revengeData [i].party.MobileParty);
                                    revengeData [i].Begin();
                                }
                            }
                            else
                            {
                                revengeData [i].Stop();
                            }
                        }
                        else
                        {
                            revengeData [i].Begin();
                        }
                    }
                }

                if (revengeData[i].state == PeasantRevengeData.quest_state.begin)
                {
                    if (revengeData[i].executioner != null)
                    {
                        if (revengeData[i].xParty != null)
                        {
                            if(revengeData [i].targetHero.HeroObject.PartyBelongedTo!=null)
                            {
                                if(Hero.MainHero.PartyBelongedTo!=null&&revengeData [i].party!=null&&revengeData [i].party==Hero.MainHero.PartyBelongedTo.Party)
                                {// Main hero as capturer 
                                    revengeData [i].Start();
                                    if(revengeData [i].xParty.Position2D.Distance(revengeData [i].targetHero.HeroObject.PartyBelongedTo.Position2D)>_cfg.values.peasantRevengePartyTalkToLordDistance)
                                    {
                                        revengeData [i].xParty.Ai.SetMoveGoToPoint(revengeData [i].targetHero.HeroObject.PartyBelongedTo.Position2D);
                                    }
                                }
                                else
                                {// Main hero as prisoner
                                    if(revengeData [i].xParty.Position2D.Distance(revengeData [i].targetHero.HeroObject.PartyBelongedTo.Position2D)>_cfg.values.peasantRevengePartyTalkToLordDistance)
                                    {
                                        revengeData [i].xParty.Ai.SetMoveGoToPoint(revengeData [i].targetHero.HeroObject.PartyBelongedTo.Position2D);
                                    }
                                    else
                                    {
                                        if(revengeData [i].criminal==Hero.MainHero.CharacterObject&&revengeData [i].party!=null&&
                                            Hero.MainHero.PartyBelongedToAsPrisoner!=null&&
                                            revengeData [i].party==Hero.MainHero.PartyBelongedToAsPrisoner)
                                        {
                                            revengeData [i].Start();
                                            if(revengeData [i].Can_peasant_revenge_lord_start)
                                            {
                                                CampaignMapConversation.OpenConversation(
                                                    new ConversationCharacterData(Hero.MainHero.CharacterObject,null,false,false,false,false,false,false),
                                                    new ConversationCharacterData(revengeData [i].party.Owner.CharacterObject,revengeData [i].party,false,false,false,false,false,false));
                                                break;
                                            }
                                            else
                                            {
                                                revengeData [i].Stop();
                                            }
                                        }
                                        else
                                        {
                                            if(revengeData [i].targetHero!=Hero.MainHero.CharacterObject)
                                            {
                                                if(RevengeAI(revengeData [i])) // if player dialog start after AI run
                                                {
                                                    revengeData [i].Start();
                                                    revengeData [i].nobleParty=revengeData [i].party;
                                                    revengeData [i].targetHero=Hero.MainHero.CharacterObject;
                                                }
                                                else
                                                {
                                                    revengeData [i].Stop();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                revengeData [i].Stop(); // target party dissapeared after begin state. 
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
                            else
                            {
                                revengeData [i].Stop(); // incorrect state - party null , but parties are enabled
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
                            if(!revengeData [i].executioner.HeroObject.HomeSettlement.IsUnderRaid)
                            {
                                if(revengeData [i].xParty.MapEvent==null) // crash during battle update map event, if not checked
                                {
                                    DestroyPartyAction.ApplyForDisbanding(revengeData [i].xParty,revengeData [i].executioner.HeroObject.HomeSettlement); // will set clear flag in events
                                }
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
                // remove here, because other events may interrupt this event 
                for(int i = 0; i<revengeData.Count; i++)
                {
                    if(revengeData[i].state ==PeasantRevengeData.quest_state.clear)
                    {
                        if(revengeData [i].xParty == null)
                        {
                            revengeData.RemoveAt(i);
                        }
                        else if(revengeData [i].xParty.MapEvent == null) // crash during update map event, if not checked
                        {
                            revengeData.RemoveAt(i);
                        }
                    }
                }
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
                    LogMessage.Add("{=PRev0042}{PARTYOWNER.NAME} decided not to execute {PRISONER.NAME} to avoid a cross-border incident with another kingdom");
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
                bool party_let_revenge_con = (!party_help_criminal_con && !party_friend_to_criminal_con && !party_relatives_with_criminal_condition && party_let_due_accusations) || party_overide_con;

                if (party_let_revenge_con || _cfg.values.alwaysExecuteTheCriminal) //no conflict with party leader and peasant or override
                {
                    bool sellement_owner_relatives_with_criminal_condition = (settlement.Owner.Children.Contains(prisoner) || prisoner.Children.Contains(settlement.Owner)) &&
                                                   CheckConditions(settlement.Owner, prisoner, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal);
                    bool sellement_owner_help_criminal_con = CheckConditions(settlement.Owner, executioner, _cfg.values.ai.lordWillAffordToHelpTheCriminalEnemy);
                    bool sellement_owner_friend_to_criminal_con = settlement.Owner.IsFriend(prisoner);
                    bool sellement_owner_overide_con = CheckConditions(settlement.Owner, executioner, _cfg.values.ai.settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet);
                    bool sellement_owner_let_due_accusations = revenge.accused_hero != null ? !AIwillMakeNoDecisionDueConflict(settlement.Owner, revenge) : true;
                    bool sellement_owner_let_revenge_con = (!sellement_owner_help_criminal_con && !sellement_owner_friend_to_criminal_con && !sellement_owner_relatives_with_criminal_condition && sellement_owner_let_due_accusations) || sellement_owner_overide_con;

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
                                            LogMessage.Add("{=PRev0043}{PARTYOWNER.NAME} decided not to execute {PRISONER.NAME} after {SAVER.NAME} executed the notable peasant {EXECUTIONER.NAME}");
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

                                LogMessage.Add("{=PRev0041}{PARTYOWNER.NAME} decided not to execute {PRISONER.NAME} after {PRISONER.NAME} paid {REPARATION}{GOLD_ICON} in reparation");
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
                            if (!sellement_owner_overide_con) condition +="was not over rided by settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet";
                            if (!sellement_owner_let_due_accusations) condition +="was not over rided by sellement_owner_let_due_accusations";
                            message = $"Settlement owner {settlement.Owner.Name} refused to support {executioner.Name}'s revenge against {prisoner.Name}. ({condition})";
                            revenge.quest_Results.Add(PeasantRevengeData.quest_result.village_denied);
                            if (sellement_owner_friend_to_criminal_con)
                            {
                                LogMessage.Add("{=PRev0047}{SETTLEMENTOWNER.NAME} decided not to execute {PRISONER.NAME} in honor of their friendship");
                            }
                            else if (sellement_owner_help_criminal_con)
                            {
                                LogMessage.Add("{=PRev0056}{SETTLEMENTOWNER.NAME} decided not to execute {PRISONER.NAME} in honor of their good relationship");
                            }
                            else if (sellement_owner_relatives_with_criminal_condition)
                            {
                                LogMessage.Add("{=PRev0057}{SETTLEMENTOWNER.NAME} decided not to execute {PRISONER.NAME} in honor of their family bonds.");
                            }
                            else if (!sellement_owner_let_due_accusations)
                            {
                                LogMessage.Add("{=PRev0104}{SETTLEMENTOWNER.NAME} decided not to execute {PRISONER.NAME} due to conflicting accusations.");
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
                    if (!party_overide_con) condition += "was not over rided by partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet";
                    if (!party_let_due_accusations) condition +="was not over rided by  party_let_due_accusations";
                    message = $"Party {party.Owner.Name} refused to support {executioner.Name}'s revenge against {prisoner.Name}. ({condition})";
                    revenge.quest_Results.Add(PeasantRevengeData.quest_result.party_denied);
                    if (party_friend_to_criminal_con)
                    {
                        LogMessage.Add("{=PRev0044}{PARTYOWNER.NAME} decided not to execute {PRISONER.NAME} in honor of their friendship");
                    }
                    else if (party_help_criminal_con)
                    {
                        LogMessage.Add("{=PRev0058}{PARTYOWNER.NAME} decided not to execute {PRISONER.NAME} in honor of their good relationship");
                    }
                    else if (party_relatives_with_criminal_condition)
                    {
                        LogMessage.Add("{=PRev0059}{PARTYOWNER.NAME} decided not to execute {PRISONER.NAME} in honor of their family bonds");
                    }
                    else if (!party_let_due_accusations)
                    {
                        LogMessage.Add("{=PRev0103}{PARTYOWNER.NAME} decided not to execute {PRISONER.NAME} due to conflicting accusations.");
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

            if (settlement.Notables.Count <= 1) // do not allow to take all notables from village, because will get crash at line 208 in  TaleWorlds.CampaignSystem.GameComponents.GetRelationIncreaseFactor() after any other criminal raid party is defeated nearby
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
            int defaultVersion = new PeasantRevengeConfiguration().CfgVersion;

            if (File.Exists(_cfg.values.file_name))
            {
                _cfg.Load(_cfg.values.file_name, typeof(PeasantRevengeConfiguration));

                if(_cfg.values.ai==null)
                {
                    _cfg.values.ai=new PeasantRevengeConfiguration.AIfilters();
                    _cfg.values.ai.Default();
                }
                else
                {
                    // configuration patch for new added configuration variables

                    if(_cfg.values.CfgVersion<14)
                    {
                        _cfg.values.ai.default_criminalWillBlameOtherLordForTheCrime();
                        _cfg.values.ai.default_lordWillKillBothAccusedHeroAndCriminalLord();
                    }

                    if(_cfg.values.CfgVersion<15)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenRansomRemainsDeclined();
                        _cfg.values.ai.default_lordTraitChangeWhenRansomRemainsAccepted();
                        _cfg.values.ai.default_lordTraitChangeWhenRemainsOfLordAreAbandoned();
                        _cfg.values.ai.default_lordWillDeclineRansomTheVictimRemains();
                        _cfg.values.ai.default_lordWillAbandonTheVictimRemains();
                    }

                    if(_cfg.values.CfgVersion<16)
                    {
                        _cfg.values.ai.default_lordWillNotKillBothAccusedHeroAndCriminalLordDueConflict();
                    }

                    if(_cfg.values.CfgVersion<17)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest();
                    }
                    if(_cfg.values.CfgVersion<19)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenLordPersuedeNotableNotToRevenge();
                        _cfg.values.ai.default_lordTraitChangeWhenLordPersuedeNotableToRevenge();
                        _cfg.values.ai.default_notableWillAcceptTheBribe();
                    }
                    if(_cfg.values.CfgVersion<20)
                    {
                        _cfg.values.ai.default_PersuadeNotableToRevengeTraitsForOption0();
                        _cfg.values.ai.default_PersuadeNotableToRevengeTraitsForOption1();
                        _cfg.values.ai.default_PersuadeNotableToRevengeTraitsForOption2();
                        _cfg.values.ai.default_PersuadeNotableNotToRevengeTraitsForOption0();
                        _cfg.values.ai.default_PersuadeNotableNotToRevengeTraitsForOption1();
                        _cfg.values.ai.default_PersuadeNotableNotToRevengeTraitsForOption2();
                        _cfg.values.ai.default_AccuseNotableTraitsForOption0();
                        _cfg.values.ai.default_AccuseNotableTraitsForOption1();
                        _cfg.values.ai.default_AccuseNotableTraitsForOption2();

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
            FindRevengesForRevengeParties();
            AddRaidingParties();
            
            //Test();
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

            log($"Total vilages {total}. Can revenge :{sum}. Average hearts: {sum_hearts / total}. MinHearts {min_hearts}. MaxHearts{max_hearts}");

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
                    var settlements = Settlement.All.Where(x =>
                    (x.LastAttackerParty != null ?
                    (x.LastAttackerParty.Owner == criminal && x.IsUnderRaid && !criminal.IsPrisoner) : false) && x.IsVillage);

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

        bool FindRevengesForRevengeParties()
        {
            bool all_found = true;

            IEnumerable<MobileParty> parties = MobileParty.AllPartiesWithoutPartyComponent.Where((x) =>
            x.IsCurrentlyUsedByAQuest&&x.StringId.StartsWith(revengerPartyNameStart));
            for(int i = 0;i<parties.Count();i++)
            {
                TroopRoster troopsLordParty = parties.ElementAt(i).MemberRoster;
                for(int j = 0;j<troopsLordParty.Count;j++)
                {
                    CharacterObject troop = troopsLordParty.GetCharacterAtIndex(j);
                    if(troop.IsHero && (troop.HeroObject.IsHeadman ||troop.HeroObject.IsRuralNotable))
                    {
                        Village village = troop.HeroObject.HomeSettlement.Village;

                        if(village!=null)
                        {
                            if( village.Settlement.LastAttackerParty!=null &&
                                village.Settlement.LastAttackerParty.Party !=null && 
                                village.Settlement.LastAttackerParty.Party.LeaderHero != null &&
                                village.Settlement.LastAttackerParty.Party.LeaderHero.PartyBelongedToAsPrisoner != null &&
                                village.Settlement.LastAttackerParty.Party.LeaderHero.PartyBelongedToAsPrisoner.LeaderHero != null
                                )
                            {
                                    IEnumerable<PeasantRevengeData> currentData = revengeData.Where((x) =>
                                       x.criminal==village.Settlement.LastAttackerParty.Party.LeaderHero.CharacterObject&&
                                       x.village==village);

                                    if(currentData.IsEmpty())
                                    {
                                        revengeData.Add(new PeasantRevengeData
                                        {
                                            executioner=troop.OriginalCharacter,
                                            reparation=(int)(village.Hearth*_cfg.values.ReparationsScaleToSettlementHearts),
                                            party=village.Settlement.LastAttackerParty.Party.LeaderHero.PartyBelongedToAsPrisoner,
                                            targetHero=village.Settlement.LastAttackerParty.Party.LeaderHero.PartyBelongedToAsPrisoner.LeaderHero.CharacterObject,
                                            village=village,
                                            criminal=village.Settlement.LastAttackerParty.Party.LeaderHero.CharacterObject,
                                            startTime=CampaignTime.DaysFromNow(0),
                                            dueTime=CampaignTime.DaysFromNow(_cfg.values.peasantRevengeTimeoutInDays),
                                            xParty=parties.ElementAt(i)
                                        });

                                        revengeData [revengeData.Count-1].xParty.Ai.SetMoveEscortParty(revengeData [revengeData.Count-1].party.MobileParty);
                                        revengeData [revengeData.Count-1].Ready();
                                        revengeData [revengeData.Count-1].Begin();
                                    }
                                
                            }
                            else
                            {
                                    revengeData.Add(new PeasantRevengeData
                                    {
                                        executioner=troop.HeroObject.CharacterObject,
                                        reparation=(int)(village.Hearth*_cfg.values.ReparationsScaleToSettlementHearts),                                     
                                        village=village,
                                        startTime=CampaignTime.DaysFromNow(0),
                                        dueTime=CampaignTime.DaysFromNow(0),
                                        xParty=parties.ElementAt(i)
                                    });
                                revengeData [revengeData.Count-1].quest_Results.Add(PeasantRevengeData.quest_result.cancelled);
                                revengeData [revengeData.Count-1].Stop();
                                revengeData [revengeData.Count-1].xParty.Ai.SetMoveGoToSettlement(revengeData [revengeData.Count-1].village.Settlement);
                            }
                        }
                    }
                }
            }

            return all_found;
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
        /// <summary>
        /// Checking hero (hero) traits and relations with another hero (target)
        /// </summary>
        /// <param name="hero">hero who has traits and relations with target hero</param>
        /// <param name="target">hero who relation is checked with hero</param>
        /// <param name="traits"></param>
        /// <returns></returns>
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

        private bool CheckOnlyTraitsConditions(Hero hero, Hero target, List<PeasantRevengeConfiguration.RelationsPerTraits> traits)
        {
            if (traits.IsEmpty()) return true;

            foreach (PeasantRevengeConfiguration.RelationsPerTraits rpt in traits)
            {
                if (hero_trait_list_condition(hero, rpt.traits, target))
                {
                    return true;
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
                            bool child_cond = (x.Children.Contains(victim) || (victim.Children.Contains(x) && x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge)) &&
                                               CheckConditions(x, victim, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal);
                            bool friend_con = x.IsFriend(victim) && CheckConditions(x, victim, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal);
                            bool not_enemy_con = !x.IsEnemy(victim);
                            bool have_gold = x.Gold >= goldNeeded;
                            if (age_con)
                            {
                                log($"Gold {Convert.ToInt32(have_gold)}\tGoldAvailable {Convert.ToInt32(money_con)}\tRelationsMin {Convert.ToInt32(relations_min_con)}\tRelationsCanHelp {Convert.ToInt32(relations_set_con)}\tRelative {Convert.ToInt32(child_cond)}\tFriend {Convert.ToInt32(friend_con)}\tNotEnemy {Convert.ToInt32(not_enemy_con)}\t{x.Name}");
                            }
                        }
                    }
                }

                list.AddRange(victim.Clan.Heroes.Where((x) =>
                  //x != victim && // can save self         
                  x.IsAlive &&
                  x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge &&
                  CanAffordToSpendMoney(x, goldNeeded, _cfg.values.ai.lordWillAffordPartOfHisSavingsToPayForFavor) &&
                  !x.IsEnemy(victim) &&
                  x.GetRelation(victim) >= _cfg.values.criminalHeroFromClanSuporterMinimumRelation && //this will block all lesser relations
                 (
                   CheckConditions(x, victim, _cfg.values.ai.lordWillAffordToHelpTheCriminalAlly) || // if not relative, friend or clan leader                 
                   x.Children.Contains(victim) || victim.Children.Contains(x) && CheckConditions(x, victim, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal) ||
                   (x.IsFriend(victim) && CheckConditions(x, victim, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal))
                  )).ToList());

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
                                    bool child_cond = (x.Children.Contains(victim) || (victim.Children.Contains(x) && x.Age >= _cfg.values.criminalHeroFromKingdomSuporterMinimumAge)) &&
                                                       CheckConditions(x, victim, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal);
                                    bool friend_con = x.IsFriend(victim) && CheckConditions(x, victim, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal);
                                    bool not_enemy_con = !x.IsEnemy(victim);
                                    bool have_gold = x.Gold >= goldNeeded;
                                    if (age_con)
                                    {
                                        log($"Gold {Convert.ToInt32(have_gold)}\tGoldAvailable {Convert.ToInt32(money_con)}\tRelationsMin {Convert.ToInt32(relations_min_con)}\tRelationsCanHelp {Convert.ToInt32(relations_set_con)}\tRelative {Convert.ToInt32(child_cond)}\tFriend {Convert.ToInt32(friend_con)}\tNotEnemy {Convert.ToInt32(not_enemy_con)}\t{x.Name}");
                                    }
                                }
                            }
                        }

                        list.AddRange(victim.Clan.Kingdom.Heroes.Where((x) =>
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
                        )).ToList());
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
                            bool child_cond = (x.Children.Contains(hero) || (hero.Children.Contains(x) && x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge)) &&
                                               CheckConditions(x, hero, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal);
                            bool friend_con = x.IsFriend(hero) && CheckConditions(x, hero, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal);
                            bool not_enemy_con = !x.IsEnemy(hero);
                            bool have_gold = x.Gold >= goldNeeded;
                            if (age_con)
                            {
                                log($"Gold {Convert.ToInt32(have_gold)}\tGoldAvailable {Convert.ToInt32(money_con)}\tRelationsMin {Convert.ToInt32(relations_min_con)}\tRelationsCanHelp {Convert.ToInt32(relations_set_con)}\tRelative {Convert.ToInt32(child_cond)}\tFriend {Convert.ToInt32(friend_con)}\tNotEnemy {Convert.ToInt32(not_enemy_con)}\t{x.Name}");
                            }
                        }
                    }
                }

                list.AddRange(hero.Clan.Heroes.Where((x) =>
                 x != hero && //victim was executed!                
                 x.IsAlive &&
                 x.Age >= _cfg.values.criminalHeroFromClanSuporterMinimumAge &&
                 CanAffordToSpendMoney(x, goldNeeded, _cfg.values.ai.lordWillAffordPartOfHisSavingsToPayForFavor) &&
                 !x.IsEnemy(hero) &&
                 x.GetRelation(hero) >= _cfg.values.criminalHeroFromClanSuporterMinimumRelation && //this will block all lesser relations
                (
                  CheckConditions(x, hero, _cfg.values.ai.lordWillAffordToHelpPayLostRansom) || // if not relative, friend or clan leader                 
                  x.Children.Contains(hero) || hero.Children.Contains(x) && CheckConditions(x, hero, _cfg.values.ai.lordIfRelativesWillHelpTheCriminal) ||
                  (x.IsFriend(hero) && CheckConditions(x, hero, _cfg.values.ai.lordIfFriendsWillHelpTheCriminal))
                 )).ToList());
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
            #region Revenger party no quest data
            campaignGameStarter.AddDialogLine(
           "peasant_revenge_any_revenger_start_ended_revenge",
           "start",
           "close_window",
           "{=PRev0129}What's there to discuss?",
           //"{=*}I've been looking for somebody, cannot remember...[if:convo_thinking][rf:idle_angry]",
           new ConversationSentence.OnConditionDelegate(this.peasant_revenge_revenger_start_no_quest_data_condition),
           () => { revengerPartiesCleanUp = true;},500,null);
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
                "{=PRev0001}You looted a nearby village. They now demand to cut someone's head off. How are you going to respond?[rf:idle_angry][ib:closed][if:idle_angry]",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_lord_start_condition), null, 100, null);
            campaignGameStarter.AddDialogLine(
                "peasant_revenge_lord_start_grievance",
                "start",
                "peasant_revenge_lord_start_grievance_received",
                "{=PRev0002}Just curious, the {PEASANTREVENGER.LINK} say that you looted your own village earlier. The peasants want your head off. How are you going to respond?[if:convo_thinking][if:idle_happy]",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_lord_start_condition_betray), null, 100, null);

            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_lord_start_grievance_requested_ask_if_not_pay",
               "peasant_revenge_lord_start_grievance_received",
               "peasant_revenge_lord_start_grievance_requested_if_not_pay_options",
               "{=PRev0062}And what if I don't pay?",
               null,
               null, 110, null, null);

            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_lord_start_grievance_requested_ask_if_not_pay",
               "peasant_revenge_lord_start_grievance_received",
               "peasant_revenge_lord_grievance_received_pay",
               "{=PRev0065}I have friends who will pay the reparation.",
               null,
               () => peasant_revenge_criminal_has_suporters_consequence(), 110,
               new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_criminal_has_suporters_clickable_condition), null);

            campaignGameStarter.AddDialogLine(
            "peasant_revenge_lord_start_grievance_requested_if_not_pay_options_die",
            "peasant_revenge_lord_start_grievance_requested_if_not_pay_options",
            "peasant_revenge_lord_start_grievance_received",
            "{=PRev0063}Peasants will have your head.[if:convo_thinking][rf:convo_grave][ib:closed]",
            () => { return Hero.MainHero.CanDie(KillCharacterAction.KillCharacterActionDetail.Executed) && will_party_leader_kill_the_criminal(); },
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
                "peasant_revenge_lord_start_grievance_denied_pay","{=PRev0004}I won't pay.",
                () => !this.peasant_revenge_lord_start_condition_betray(),
                null, 100, null, null);

            campaignGameStarter.AddPlayerLine(
                "peasant_revenge_lord_start_grievance_requested_no_betray",
                "peasant_revenge_lord_start_grievance_received",
                "peasant_revenge_lord_start_grievance_denied_pay","{=PRev0005}I won't compensate a rat!",
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
             "{=PRev0100}I cannot decide...[if:convo_thinking][ib:closed]",
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
             "{=PRev0077}You both deserve the peasants revenge![rf:idle_angry][ib:closed]",
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
             "{=PRev0010}Well, maybe it is not for peasants to decide your fate...[if:convo_thinking]",
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
            "{=PRev0012}Well I am satisfied with that.[ib:happy]",
            () => { return Hero.MainHero.CanDie(KillCharacterAction.KillCharacterActionDetail.Executed) && will_party_leader_kill_the_criminal(); },
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
                if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) &&
                   currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
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
             "{=PRev0011}Pay for your crime![rf:idle_angry][if:convo_bored]",
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
             "{=PRev0012}Well I am satisfied with that.[ib:happy]",
             new ConversationSentence.OnConditionDelegate(this.barter_successful_condition),
             new ConversationSentence.OnConsequenceDelegate(peasant_revenge_player_payed_consecuence), 100, null);
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_lord_start_grievance_not_received_pay",
              "peasant_revenge_lord_grievance_received_pay",
              "peasant_revenge_lord_start_grievance_received",
              "{=PRev0013}That is quite unfortunate.[ib:warrior][if:convo_bored][rf:convo_grave]", () => !this.barter_successful_condition(),
              null, 100, null);

            #endregion

            #region When player captured the criminal
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_start_grievance",
               "start",
               "peasant_revenge_peasants_start_grievance_received",
               "{=PRev0014}Your prisoner {CRIMINAL.LINK} looted our village. We demand to impale their head on a spike![if:convo_furious][ib:aggressive]",
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
               "{=PRev0017}No, it is not your business. Peasant!", null,
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
              "{=PRev0073}I swear! It was all {CVICTIM.LINK}'s plan![rf:convo_grave][ib:closed]",
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
               "{=PRev0077}You both deserve the peasants revenge!",
               null, () =>
               {
                   currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.accused_hero_killed);
                   currentRevenge.quest_Results.Add(PeasantRevengeData.quest_result.criminal_killed);
               }, 90, null, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_peasants_ask_criminal_option_2",
               "peasant_revenge_peasants_ask_criminal_options",
               "peasant_revenge_peasants_finish_criminal_comment",
               "{=PRev0098}I cannot decide...",
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
             () => { return currentRevenge.executioner.HeroObject == Hero.OneToOneConversationHero; },
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
                  if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) &&
                    currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
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
                  if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) ||
                    currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
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
                 return
                    currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) ||
                    currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed)
                    ;
             }, () =>
             {
                 if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed) &&
                    currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
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
               "{=PRev0022}I will not bow to some peasants demands! And {HERO.NAME} shouldn't either!",
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
               "{=PRev0023}I won't pay! The criminal {CVICTIM.NAME} will die!", () =>
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
               "{=PRev0028}At last! The criminal will die for their sins.[ib:closed][if:happy]",
               new ConversationSentence.OnConditionDelegate(peasant_revenge_party_need_compensation_for_killed_pow_condition),
               () => { peasant_revenge_party_need_compensation_for_killed_pow_consequence(); leave_encounter(); }, 120, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_peasants_finish_not_paid_no_compensation",
               "peasant_revenge_peasants_messenger_finish_not_paid",
               "close_window",
               "{=PRev0029}The criminal lord is dead! Huzza![ib:closed][if:happy]",
                () => !peasant_revenge_party_need_compensation_for_killed_pow_condition(),
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_end_revenge_consequence), 120, null);
            #endregion

            #region Prisoner party demands compensation because no ransom
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_party_need_compensation_start",
               "start",
               "peasant_revenge_party_need_compensation_ask_support",
               "{=PRev0030}See that? Our prisoner was just killed by a peasant![ib:convo_bared_teeth][if:convo_shocked][if:convo_astonished]",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_party_need_compensation_condition),
               null, 120, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_party_need_compensation_support",
               "peasant_revenge_party_need_compensation_ask_support",
               "peasant_revenge_party_need_compensation_player_options",
               "{=PRev0031}{?GIFT_RECEIVER.GENDER}Lady{?}Lord{\\?} {GIFT_RECEIVER.LINK} is demanding a ransom of {RANSOM_COMPENSATION}{GOLD_ICON}.[ib:convo_nervous][if:convo_grave]",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_party_need_compensation_gift_condition),
               null, 120, null);

            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_party_need_compensation_player_options_0",
             "peasant_revenge_party_need_compensation_player_options",
             "peasant_revenge_party_need_compensation_denied",
             "{=PRev0032}You won't get a thing out of me!",
             null,
             null, 115, null);
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_party_need_compensation_player_options_1",
              "peasant_revenge_party_need_compensation_player_options",
               "peasant_revenge_party_need_compensation_barter_reaction",
              "{=PRev0033}Please deliver this gift to {?GIFT_RECEIVER.GENDER}Lady{?}Lord{\\?} {GIFT_RECEIVER.NAME}.",
              new ConversationSentence.OnConditionDelegate(this.peasant_revenge_party_get_compensation_gift_condition), null
              , 110, null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_party_need_compensation_player_options_2",
               "peasant_revenge_party_need_compensation_player_options",
               "peasant_revenge_party_need_compensation_denied_party_killed",
               "{=PRev0034}The captive is dead! About time you join them.",
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
             "{=PRev0038}This is unfair.[ib:closed][if:idle_angry]", null,
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

            #region Peasant revenge configuration via dialog

#warning imprisonment result in crash (only when all? )
            //campaignGameStarter.AddPlayerLine(
            //   "peasant_revenge_player_config_mod_start",
            //   "peasant_revenge_player_not_happy_with_peasant_start_options",//"hero_main_options",
            //   "peasant_revenge_player_config_mod_options_set",
            //   "{=PRev0095}There is something I'd like to discuss.",
            //   new ConversationSentence.OnConditionDelegate(this.peasant_revenge_player_config_mod_start_condition),null,100,null);
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_player_config_mod_npc_options",
              "peasant_revenge_player_config_mod_options_set",
              "peasant_revenge_player_config_mod_options_set",
              "{=PRev0084}Yes, my {?MAINHERO.GENDER}Lady{?}Lord{\\?}.[rf:convo_thinking]",null,null,200,null);
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_config_mod_option_mp_dis",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "peasant_revenge_player_config_mod_end_dis",
               "{=PRev0081}You should not immediately interrupt me with any of your matters.",
                () => { return !_cfg.values.enableRevengerMobileParty; },() => { SetEnableRevengerMobileParty(true); },
                110,
                new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_party_clickable_condition));
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_config_mod_option_mp_en",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "peasant_revenge_player_config_mod_end_en",
               "{=PRev0082}You should immediately interrupt me with any of your matters.",
                () => { return _cfg.values.enableRevengerMobileParty; },() => { SetEnableRevengerMobileParty(false); },
                110,
                new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_party_clickable_condition));
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_player_config_mod_option_np_en",
              "peasant_revenge_player_not_happy_with_peasant_start_options",
              "peasant_revenge_player_config_mod_end_dis",
              "{=PRev0092}I will defend villages from any looters.",
               () => { return !_cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu; },() => { SetEnableHelpNeutralVillage(true); },100,
               new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_neutral_village_attack_clickable_condition));
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_player_config_mod_option_np_dis",
              "peasant_revenge_player_not_happy_with_peasant_start_options",
              "peasant_revenge_player_config_mod_end_en",
              "{=PRev0093}I will defend villages from my enemies only.",
               () => { return _cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu; },() => { SetEnableHelpNeutralVillage(false); },100,
               new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_neutral_village_attack_clickable_condition));           

            campaignGameStarter.AddDialogLine(
             "peasant_revenge_player_config_mod_npc_end_dis",
             "peasant_revenge_player_config_mod_end_dis",
             "peasant_revenge_player_not_happy_with_peasant_start_options",
             "{=PRev0084}Yes, my {?MAINHERO.GENDER}Lady{?}Lord{\\?}.[rf:idle_happy]",
             () => { StringHelpers.SetCharacterProperties("MAINHERO",Hero.MainHero.CharacterObject); return true; },null,200,null);
            campaignGameStarter.AddDialogLine(
             "peasant_revenge_player_config_mod_npc_end_en",
             "peasant_revenge_player_config_mod_end_en",
             "peasant_revenge_player_not_happy_with_peasant_start_options",
             "{=PRev0084}Yes, my {?MAINHERO.GENDER}Lady{?}Lord{\\?}.[rf:idle_angry][ib:closed]",
             () => { StringHelpers.SetCharacterProperties("MAINHERO",Hero.MainHero.CharacterObject); return true; },null,200,null);

            #endregion
            #region Peasants has no traits to resist
            #region start
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_ask",
               "hero_main_options",
               "peasant_revenge_player_not_happy_with_peasant_start_options_eset",
               "{=PRev0048}Will you deal with the criminals in this village?",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_start_condition),
               null/*() => { SetHeroTraitValue(Hero.MainHero, "Valor", -2); SetHeroTraitValue(Hero.MainHero, "Mercy", 2); }*/
               , 100, null);
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_player_not_happy_with_peasant_start_peasant",
               "peasant_revenge_player_not_happy_with_peasant_start_options_eset",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "{=PRev0124}Yes. Why do you ask?[ib:closed][rf:convo_thinking]",
               () => { return notable_can_do_revenge(Hero.OneToOneConversationHero); }, null, 100, null);
            campaignGameStarter.AddDialogLine(
                "peasant_revenge_player_not_happy_with_peasant_start_peasant",
                "peasant_revenge_player_not_happy_with_peasant_start_options_eset",
                "peasant_revenge_player_not_happy_with_peasant_start_options",
                "{=PRev0049}No, I do not care.[ib:closed]",
                () => { return !notable_can_do_revenge(Hero.OneToOneConversationHero); }, null, 100, null);
            #endregion
            #region options
#warning Add crime rating increase if executed noble
            //EXECUTE
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_fast",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
                "peasant_revenge_player_not_happy_with_peasant_start_persuasion",
               "{=PRev0050}Your criminal intentions are well known to me.", null,
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_player_not_happy_with_peasant_accuse_consequence)
               , 120,
               new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_execute_clickable));
            //TEACH
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_teach",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "peasant_revenge_player_not_happy_with_peasant_start_persuasion",
               "{PAYER_COMMENT_REVENGE_TEACH}",
               new ConversationSentence.OnConditionDelegate(peasant_revenge_player_not_happy_with_peasant_teach_condition),
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_player_not_happy_with_peasant_teach_consequence),
               125,
               new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_start_teach_clickable));
            //BRIBE
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_give",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "peasant_revenge_player_not_happy_with_peasant_post_learned",
               "{PAYER_COMMENT_REVENGE_BRIBE}",
               new ConversationSentence.OnConditionDelegate(peasant_revenge_player_not_happy_with_peasant_bribe_condition),
               new ConversationSentence.OnConsequenceDelegate(peasant_revenge_player_not_happy_with_peasant_bribe_consequence),
               130,
               new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_start_bribe_clickable));
            //Leave
            campaignGameStarter.AddPlayerLine(
               "peasant_revenge_player_not_happy_with_peasant_start_give_leave",
               "peasant_revenge_player_not_happy_with_peasant_start_options",
               "close_window",
               "{=PRev0083}Nevermind.",
               null,
               () => leave_encounter(), 80, null);
            #endregion
            #region ending
            // CAN REVENGE SUCCESS
            campaignGameStarter.AddDialogLine(
            "peasant_revenge_player_not_happy_with_peasant_learned_1",
            "peasant_revenge_player_not_happy_with_peasant_post_learned",
            "close_window",
            "{=PRev0120}They are breaking the law and will rightfully face the consequences of their actions.[ib:closed][if:angry]",
             () => peasant_revenge_player_not_happy_with_peasant_post_learned_can_revenge_on_condition(),
             () => { peasant_revenge_player_not_happy_with_peasant_teaching_consequence(); leave_encounter(); },
             100, null);
            // CANNOT REVENGE SUCCESS
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_player_not_happy_with_peasant_learned_2",
              "peasant_revenge_player_not_happy_with_peasant_post_learned",
              "close_window",
              "{=PRev0054}They can do what they want. It is not my business to interfere.[ib:closed][if:angry]",
              () => peasant_revenge_player_not_happy_with_peasant_post_learned_not_revenge_on_condition(),
              () => { peasant_revenge_player_not_happy_with_peasant_teaching_consequence();
                      StopRevengeForNotableIfAny(Hero.OneToOneConversationHero);
                      leave_encounter(); },
              100, null);
            //BOTH NOT SUCCESS
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_player_not_happy_with_peasant_learned_3",
              "peasant_revenge_player_not_happy_with_peasant_post_learned",
              "close_window",
              "{=PRev0117}Enough, my decision is final![ib:closed][if:convo_bared_teeth][if:idle_angry]",
              () => peasant_revenge_player_not_happy_with_peasant_post_learned_refuse_on_condition(),
              () => { peasant_revenge_player_not_happy_with_peasant_teaching_consequence(); leave_encounter(); },// this change must be then persuation fail or success //() => { ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, Hero.OneToOneConversationHero, _cfg.values.relationChangeWhenLordTeachPeasant, true); },
              100, null);
            // PERSUATION FAIL
            campaignGameStarter.AddDialogLine(
              "peasant_revenge_player_not_happy_with_peasant_learned_4",
              "peasant_revenge_player_not_happy_with_peasant_post_learned",
              "close_window",
              "{FAIL_PERSUADE_LINE}",
              () => peasant_revenge_player_not_happy_with_peasant_post_learned_fail_on_condition(),
              () => { peasant_revenge_player_not_happy_with_peasant_teaching_consequence(); leave_encounter(); },
              100, null);
            //ACCUSATION SUCCESS
            //TODO; Here could be an option to spare the peasant (but it is already over extended)
            campaignGameStarter.AddDialogLine(
               "peasant_revenge_player_not_happy_with_peasant_learned_5",
               "peasant_revenge_player_not_happy_with_peasant_post_learned",
               "peasant_revenge_player_not_happy_with_peasant_end_accusation_options",
               "{=PRev0125}I admit.[if:convo_thinking][if:convo_grave]",
               () => peasant_revenge_player_not_happy_with_peasant_post_accusation_success_on_condition(),
               null, 100, null);
            //TODO; After execution, could be an angry letter from peasant owner (but it is already over extended)
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_player_not_happy_with_peasant_end_accusation",
              "peasant_revenge_player_not_happy_with_peasant_end_accusation_options",
               "close_window",
              "{=PRev0109}I will chop your head off!",
              null,
              () => { peasant_revenge_player_not_happy_with_peasant_chop_consequence(); leave_encounter_and_mission(); }
              , 90,
             new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_end_accusation_clickable));
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_player_not_happy_with_peasant_end_accusation_companion",
              "peasant_revenge_player_not_happy_with_peasant_end_accusation_options",
               "close_window",
              "{=PRev0126}{EXECUTIONER.LINK} will chop your head off!",
              () => { return peasant_revenge_get_executioner_companion_condition(); },
              () => { peasant_revenge_player_not_happy_with_peasant_companion_chop_consequence(get_first_companion()); leave_encounter_and_mission(); }
              , 100,
              new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_end_accusation_companion_clickable));
            campaignGameStarter.AddPlayerLine(
              "peasant_revenge_player_not_happy_with_peasant_end_accusation_spare",
              "peasant_revenge_player_not_happy_with_peasant_end_accusation_options",
               "close_window",
              "{=PRev0127}Justice demands you pay for your crimes.",
              () => { return true; },
              () => { peasant_revenge_player_not_happy_with_peasant_companion_take_notable_prisoner_consequence(); leave_encounter_and_mission(); }
              ,110,
              new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_companion_take_notable_prisoner_clickable));
            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_player_not_happy_with_peasant_end_accusation_exit",
             "peasant_revenge_player_not_happy_with_peasant_end_accusation_options",
             "close_window",
             "{=PRev0094}I must leave now.",null,() => leave_encounter(),
             0,null);
            //TEACH
            #endregion
            #endregion

            Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotablePersuasionDialogFlow(), this);
        }

        private bool peasant_revenge_player_not_happy_with_peasant_end_accusation_companion_clickable(out TextObject explanation)
        {
            explanation=null;
            return can_remove_notable_from_village();
        }
        private bool peasant_revenge_player_not_happy_with_peasant_end_accusation_clickable(out TextObject explanation)
        {
            explanation=null;
            return can_remove_notable_from_village();
        }

        private bool can_remove_notable_from_village()
        {
            return (Hero.OneToOneConversationHero!=null
                && Hero.OneToOneConversationHero.HomeSettlement!= null &&
                Hero.OneToOneConversationHero.HomeSettlement.Notables != null &&
                Hero.OneToOneConversationHero.HomeSettlement.Notables.Count>1);
        }
        #region peasant revenge persuede

        #region persuation task

        PersuasionTask _task;

        private DialogFlow GetNotablePersuasionDialogFlow()
        {
            DialogFlow dialog = DialogFlow.CreateDialogFlow("peasant_revenge_player_not_happy_with_peasant_start_persuasion", 125);

            dialog.AddDialogLine(
                "peasant_revenge_player_not_happy_with_peasant_learn_started",
                "peasant_revenge_player_not_happy_with_peasant_start_persuasion",
                "peasant_revenge_persuasion_start_reservation",
                "{=!}{PEASANT_COMMENT_LINE}",
                new ConversationSentence.OnConditionDelegate(this.persuasion_start_with_notable_on_condition),
                new ConversationSentence.OnConsequenceDelegate(this.persuasion_start_with_notable_on_consequence),
                this, 100, null, null, null);

            dialog.AddDialogLine(
               "peasant_revenge_persuasion_rejected",
               "peasant_revenge_persuasion_start_reservation",
               "close_window",
               "{=!}{TRY_LATER_PERSUASION_LINE}",
               new ConversationSentence.OnConditionDelegate(this.peasant_revenge_persuasion_rejected_on_condition),
               () => {
                   this.peasant_revenge_persuasion_rejected_on_consequence();
                   leave_encounter();
               },
               this, 100, null,
               new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero),
               new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsNotableHero));

            dialog.AddDialogLine(
                "peasant_revenge_persuasion_failed",
                "peasant_revenge_persuasion_start_reservation",
                "peasant_revenge_player_not_happy_with_peasant_post_learned",
                "{=!}{FAILED_PERSUASION_LINE}",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_persuasion_failed_on_condition),
                new ConversationSentence.OnConsequenceDelegate(this.peasant_revenge_persuasion_failed_on_consequence),
                this, 100, null,
                new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero),
                new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsNotableHero));

            dialog.AddDialogLine(
                "peasant_revenge_persuasion_success",
                "peasant_revenge_persuasion_start_reservation",
                "peasant_revenge_player_not_happy_with_peasant_post_learned",
                "{=PRev0128}You're right.",
                new ConversationSentence.OnConditionDelegate(ConversationManager.GetPersuasionProgressSatisfied),
                new ConversationSentence.OnConsequenceDelegate(this.peasant_revenge_persuasion_success_on_consequence),
                this, int.MaxValue, null,
                new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero),
                new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsNotableHero));

            dialog.AddDialogLine("peasant_revenge_persuasion_attempt",
                "peasant_revenge_persuasion_start_reservation",
                "peasant_revenge_persuasion_select_option",
                "{=PRev0129}What's there to discuss?",
                () => { return persuade_not_failed_on_condition(); },
                null, this, 10, null, null, null);

            #region OPTIONS           
            dialog.AddPlayerLine(
                    "peasant_revenge_persuasion_select_option_0",
                    "peasant_revenge_persuasion_select_option",
                    "peasant_revenge_persuasion_select_option_response",
                    "{=!}{REVENGER_PERSUADE_OPTION_0}",
                    () => { return this.persuasion_select_option_i_on_condition(0); },
                    () => { persuasion_select_option_i_on_consequence(0); },
                    this, 100,
                    (out TextObject hintText) => { return this.persuasion_clickable_option_i_on_condition(0, out hintText); },
                    () => { return this.persuasion_setup_option_i(0); },
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero),
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsNotableHero));
            dialog.AddPlayerLine(
                    "peasant_revenge_persuasion_select_option_1",
                    "peasant_revenge_persuasion_select_option",
                    "peasant_revenge_persuasion_select_option_response",
                    "{=!}{REVENGER_PERSUADE_OPTION_1}",
                    () => { return this.persuasion_select_option_i_on_condition(1); },
                    () => { persuasion_select_option_i_on_consequence(1); },
                    this, 100,
                    (out TextObject hintText) => { return this.persuasion_clickable_option_i_on_condition(1, out hintText); },
                    () => { return this.persuasion_setup_option_i(1); },
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero),
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsNotableHero));
            dialog.AddPlayerLine(
                    "peasant_revenge_persuasion_select_option_2",
                    "peasant_revenge_persuasion_select_option",
                    "peasant_revenge_persuasion_select_option_response",
                    "{=!}{REVENGER_PERSUADE_OPTION_2}",
                    () => { return this.persuasion_select_option_i_on_condition(2); },
                    () => { persuasion_select_option_i_on_consequence(2); },
                    this, 100,
                    (out TextObject hintText) => { return this.persuasion_clickable_option_i_on_condition(2, out hintText); },
                    () => { return this.persuasion_setup_option_i(2); },
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsMainHero),
                    new ConversationSentence.OnMultipleConversationConsequenceDelegate(this.IsNotableHero));
            #endregion
            //RESPONSE
            dialog.AddDialogLine(
                "peasant_revenge_persuasion_select_option_reaction",
                "peasant_revenge_persuasion_select_option_response",
                "peasant_revenge_persuasion_start_reservation",
                "{=*}{PERSUASION_REACTION}",
                new ConversationSentence.OnConditionDelegate(this.persuasion_selected_option_response_on_condition),
                new ConversationSentence.OnConsequenceDelegate(this.persuasion_selected_option_response_on_consequence),
                this, 100, null, null, null);

            return dialog;
        }

        private Tuple<TraitObject,int> [] GetTraitCorrelations(int valor = 0,int mercy = 0,int honor = 0,int generosity = 0,int calculating = 0)
        {
            return new Tuple<TraitObject,int> []
            {
                new Tuple<TraitObject, int>(DefaultTraits.Valor, valor),
                new Tuple<TraitObject, int>(DefaultTraits.Mercy, mercy),
                new Tuple<TraitObject, int>(DefaultTraits.Honor, honor),
                new Tuple<TraitObject, int>(DefaultTraits.Generosity, generosity),
                new Tuple<TraitObject, int>(DefaultTraits.Calculating, calculating)
            };
        }

        private PersuasionArgumentStrength GetPersuationArgumentStrength(CharacterObject target_hero, List<PeasantRevengeConfiguration.TraitAndValue> traits_and_values)
        {
            int valor = 0, mercy = 0, honor = 0, generosity = 0, calculating = 0;

            foreach(PeasantRevengeConfiguration.TraitAndValue tv in traits_and_values )
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
            PersuasionArgumentStrength argstr = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(target_hero, traitCorrelations); // how much argument trait tuple correlates with npc and player  

            PersuasionDifficulty min_difficulty = PersuasionDifficulty.Medium;

            PersuasionDifficulty difficulty = GetStartPersuasionDifficulty(Hero.MainHero,target_hero.HeroObject,min_difficulty);

            argstr = argstr - (difficulty - min_difficulty);

            if(argstr<PersuasionArgumentStrength.ExtremelyHard)
            {
                argstr=PersuasionArgumentStrength.ExtremelyHard;
            }
            else if(argstr>PersuasionArgumentStrength.ExtremelyEasy)
            {
                argstr=PersuasionArgumentStrength.ExtremelyEasy;
            }

            return argstr;
        }

        private PersuasionTask GetPersuasionTask(int task_index)
        {
            PersuasionTask persuasionTask = new PersuasionTask(0);

            persuasionTask.FinalFailLine = new TextObject("{=PRev0131}I think...[ib:thinking]", null);
            persuasionTask.TryLaterLine = new TextObject("{=PRev0078}I do not have time to talk right now.[rf:idle_angry][ib:closed][if:idle_angry]", null);
            persuasionTask.SpokenLine = new TextObject("{=PRev0130}Maybe...", null);

            if (task_index == 0)
            {
                PersuasionOptionArgs option0 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Valor, TraitEffect.Positive, 
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,_cfg.values.ai.PersuadeNotableToRevengeTraitsForOption0),
                    false, new TextObject("{=PRev0132}No one should be afraid of these criminals.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option0);
                PersuasionOptionArgs option1 = new PersuasionOptionArgs(DefaultSkills.Engineering, DefaultTraits.Mercy, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,_cfg.values.ai.PersuadeNotableToRevengeTraitsForOption1),
                    false, new TextObject("{=PRev0133}Someone must be held accountable for the destruction of our village!", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option1);
                PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Negative,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,_cfg.values.ai.PersuadeNotableToRevengeTraitsForOption2),
                    false, new TextObject("{=PRev0134}Take justice into your own hands!", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option2);
            }
            else if (task_index == 1)
            {
                PersuasionOptionArgs option0 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Valor, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,_cfg.values.ai.PersuadeNotableNotToRevengeTraitsForOption0),
                    false, new TextObject("{=PRev0135}These criminals are too dangerous.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option0);
                PersuasionOptionArgs option1 = new PersuasionOptionArgs(DefaultSkills.Engineering, DefaultTraits.Mercy, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,_cfg.values.ai.PersuadeNotableNotToRevengeTraitsForOption1),
                    false, new TextObject("{=PRev0136}Pity for your enemy is cruelty onto your ally.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option1);
                PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive,
                     GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,_cfg.values.ai.PersuadeNotableNotToRevengeTraitsForOption2),
                    false, new TextObject("{=PRev0137}Let the nobles take care of the judgement. You are not important enough.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option2);
            }
            else if (task_index == 2)
            {
                PersuasionOptionArgs option0 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,_cfg.values.ai.AccuseNotableTraitsForOption0),
                    false, new TextObject("{=PRev0138}Everyone has heard of your hostile speeches against nobles.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option0);
                PersuasionOptionArgs option1 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Mercy, TraitEffect.Negative,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,_cfg.values.ai.AccuseNotableTraitsForOption1),
                    false, new TextObject("{=PRev0139}Your kindness to the enemy is harmful enough to consider it criminal.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option1);
                PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject,_cfg.values.ai.AccuseNotableTraitsForOption2),
                    false, new TextObject("{=PRev0140}Everyone knows I'm telling the truth.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option2);
            }

            return persuasionTask;
        }

        private bool persuasion_start_with_notable_on_condition()
        {
            if (this._task.Options.Count > 0)
            {
                TextObject textObject = new TextObject("{=*}{COMMENT_LINE}", null);

                if (persuade_status == persuade_type.accusation)
                {
                    textObject.SetTextVariable("COMMENT_LINE", new TextObject("{=PRev0141}Your accusation is baseless.[ib:nervous][if:convo_astonished]", null));
                }
                else
                {
                    textObject.SetTextVariable("COMMENT_LINE", new TextObject("{=PRev0142}Do not expect me to change my ways.", null));
                }

                MBTextManager.SetTextVariable("PEASANT_COMMENT_LINE", textObject, false);
                return true;
            }
            return false;
        }

        private PersuasionDifficulty GetStartPersuasionDifficulty( Hero hero_initiator, Hero hero_target,PersuasionDifficulty min_difficulty)
        {
            PersuasionDifficulty diff = min_difficulty;

            bool can_revenge = notable_can_do_revenge(hero_target);
            bool main_have_exclude_trait = hero_trait_list_condition(hero_initiator,_cfg.values.peasantRevengerExcludeTrait);
            bool can_revenge_have_ex_traits = can_revenge && main_have_exclude_trait;
            bool cannot_revenge_have_no_ex_traits = !can_revenge && !main_have_exclude_trait;           
            bool have_traits = can_revenge_have_ex_traits||cannot_revenge_have_no_ex_traits;

            if(!have_traits)
            {
                diff += 1;
            }

            if(hero_initiator.MapFaction != hero_target.MapFaction)
            {
                diff += 1;
            }

            if(hero_initiator.MapFaction.IsAtWarWith(hero_target.MapFaction))
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

            if(hero_target.IsEnemy(hero_initiator))
            {
                diff += 1;
            }
            else if(hero_target.IsFriend(hero_initiator))
            {
                diff -= 1;
            }

            if(diff > PersuasionDifficulty.Impossible)
            {
                diff=PersuasionDifficulty.Impossible;
            }

            return diff;
        }

        private void persuasion_start_with_notable_on_consequence()
        {
            if (persuade_status == persuade_type.accusation)
            {
                
                ConversationManager.StartPersuasion(1f, 1f, 0f, 1f, 1f, 0f, PersuasionDifficulty.Hard);
            }
            else
            {                
                ConversationManager.StartPersuasion(2f, 1f, 0f, 2f, 2f, 0f, PersuasionDifficulty.Hard);
            }
        }

        private void peasant_revenge_player_not_happy_with_peasant_teach_consequence()
        {
            bool can_revenge = notable_can_do_revenge(Hero.OneToOneConversationHero);
            int task_index = 0;

            if (can_revenge)
            {
                task_index = 1;
                persuade_status = persuade_type.teach_to_not_revenge;
            }
            else
            {
                persuade_status = persuade_type.teach_to_revenge;
            }

            _task = GetPersuasionTask(task_index);
            _task.UnblockAllOptions();

            add_notable_persuaded_count();
        }

        private void peasant_revenge_player_not_happy_with_peasant_accuse_consequence()
        {
            persuade_status = persuade_type.accusation;
            _task = GetPersuasionTask(2);
            _task.UnblockAllOptions();
        }
        #region the same for all options
        private bool persuasion_select_option_i_on_condition(int option_index)
        {
            if (this._task.Options.Count > 0)
            {
                TextObject textObject = new TextObject("{=*}{OPTION_LINE} {SUCCESS_CHANCE}", null);
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._task.Options.ElementAt(option_index), false));
                textObject.SetTextVariable("OPTION_LINE", this._task.Options.ElementAt(option_index).Line);
                string option_string = $"REVENGER_PERSUADE_OPTION_{option_index}";
                MBTextManager.SetTextVariable(option_string, textObject, false);
                return true;
            }
            return false;
        }

        private void persuasion_select_option_i_on_consequence(int option_index)
        {
            if (this._task.Options.Count > 0)
            {
                if (persuade_status == persuade_type.accusation)
                {
                    this._task.BlockAllOptions();
                }
                else
                {
                    this._task.Options[option_index].BlockTheOption(true);
                }
            }
        }

        private bool persuasion_clickable_option_i_on_condition(int option_index, out TextObject hintText)
        {
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            if (this._task.Options.Count > 0)
            {
                hintText = this._task.Options.ElementAt(option_index).IsBlocked ? hintText : TextObject.Empty;
                return !this._task.Options.ElementAt(option_index).IsBlocked;
            }
            return false;
        }

        private PersuasionOptionArgs persuasion_setup_option_i(int option_index)
        {
            return this._task.Options.ElementAt(option_index);
        }

        private bool persuasion_selected_option_response_on_condition()
        {
            PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
            MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
            if (item == PersuasionOptionResult.CriticalFailure)
            {
                this._task.BlockAllOptions();
            }
            return true;
        }
        private void persuasion_selected_option_response_on_consequence()
        {
            Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
            float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Medium);
            float moveToNextStageChance;
            float blockRandomOptionChance;
            Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out moveToNextStageChance, out blockRandomOptionChance, difficulty);
            this._task.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
        }

        private bool IsNotableHero(IAgent agent)
        {
            if (!(Hero.OneToOneConversationHero != null && (Hero.OneToOneConversationHero.IsHeadman || Hero.OneToOneConversationHero.IsRuralNotable)))
                return false;
            return agent.Character == Hero.OneToOneConversationHero.CharacterObject;
        }

        private bool IsMainHero(IAgent agent)
        {
            return agent.Character == CharacterObject.PlayerCharacter;
        }

        private bool persuade_not_failed_on_condition()
        {
            return !peasant_revenge_persuasion_failed_on_condition() && !ConversationManager.GetPersuasionProgressSatisfied();
        }

        private bool peasant_revenge_persuasion_failed_on_condition()
        {
            if (_task.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
            {
                MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", _task.FinalFailLine, false);
                return true;
            }
            return false;
        }

        #endregion

        private bool peasant_revenge_persuasion_rejected_on_condition()
        {
            if (peasant_revenge_player_not_happy_with_peasant_post_learned_refuse_on_condition())
            {
                MBTextManager.SetTextVariable("TRY_LATER_PERSUASION_LINE", this._task.TryLaterLine, false);
                return true;
            }
            return false;
        }

        private void peasant_revenge_persuasion_rejected_on_consequence()
        {
            ConversationManager.EndPersuasion();
            if (persuade_status == persuade_type.accusation)
            {
                persuade_status = persuade_type.accusation_fail;
            }
            else
            {
                persuade_status = persuade_type.show_example_fail;
            }
        }

        private void peasant_revenge_persuasion_failed_on_consequence()
        {
            ConversationManager.EndPersuasion();
            if (persuade_status == persuade_type.accusation)
            {
                persuade_status = persuade_type.accusation_fail;
            }
            else
            {
                persuade_status = persuade_type.show_example_fail;
            }
        }

        private void peasant_revenge_persuasion_success_on_consequence()
        {
            ConversationManager.EndPersuasion();
            if (persuade_status == persuade_type.accusation)
            {
                persuade_status = persuade_type.accusation_success;
            }
            else
            {
                persuade_status = persuade_type.show_example_success;
            }
        }

        #endregion

        private void peasant_revenge_player_not_happy_with_peasant_teaching_consequence()
        {
            if (persuade_status == persuade_type.show_example_fail || persuade_status == persuade_type.show_example_success)
            {
                if (previous_can_revenge)
                {
                    OnLordPersuedeNotableNotToRevenge(Hero.MainHero);
                }
                else
                {
                    OnLordPersuedeNotableToRevenge(Hero.MainHero);
                }

                if (persuade_status == persuade_type.show_example_success)
                {
                    TeachHeroTraits(Hero.OneToOneConversationHero, _cfg.values.peasantRevengerExcludeTrait, previous_can_revenge);
                }
            }

            if (persuade_status == persuade_type.bribe_fail || persuade_status == persuade_type.bribe_success)
            {
                //TODO: make notable traits depended negative outcome
                int relation_change = //persuade_status == persuade_type.bribe_fail ? -_cfg.values.relationChangeWhenLordBribePeasant :
               _cfg.values.relationChangeWhenLordBribePeasant;

                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, Hero.MainHero,
                relation_change,
                _cfg.values.relationChangeWhenLordBribePeasant != 0);
            }
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_can_revenge_on_condition()
        {
            if (!previous_can_revenge &&
            (persuade_status == persuade_type.bribe_success || persuade_status == persuade_type.show_example_success))
            {
                return true;
            }
            return false;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_not_revenge_on_condition()
        {
            if (previous_can_revenge && (persuade_status == persuade_type.bribe_success || persuade_status == persuade_type.show_example_success))
            {
                return true;
            }
            return false;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_refuse_on_condition()
        {
            return get_notable_persuaded_count() > _cfg.values.lordCanTryAsManyTimesToPersuadeTheNotable;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_fail_on_condition()
        {
            if (persuade_status == persuade_type.bribe_fail || persuade_status == persuade_type.show_example_fail)
            {
                TextObject textObject = new TextObject("{=*}{COMMENT_LINE}", null);
                textObject.SetTextVariable("COMMENT_LINE", new TextObject("{=PRev0121}I can't make any promises..[ib:closed]", null));
                MBTextManager.SetTextVariable("FAIL_PERSUADE_LINE", textObject, false);
                return true;
            }

            if (persuade_status == persuade_type.accusation_fail)
            {
                TextObject textObject = new TextObject("{=*}{COMMENT_LINE}", null);
                textObject.SetTextVariable("COMMENT_LINE", new TextObject("{=PRev0143}Let's say it was a misunderstanding.", null));
                MBTextManager.SetTextVariable("FAIL_PERSUADE_LINE", textObject, false);
                return true;
            }

            return false;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_accusation_success_on_condition()
        {
            return persuade_status == persuade_type.accusation_success;
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

        private int get_notable_bribe_amount()
        {
            int bribe_percents = _cfg.values.goldPercentOfPeasantTotallGoldToTeachPeasantToBeLoyal;
            int bribe = Hero.OneToOneConversationHero.Gold * bribe_percents / 100;
            return bribe;
        }

        private void peasant_revenge_player_not_happy_with_peasant_bribe_consequence()
        {
            add_notable_persuaded_count();

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, get_notable_bribe_amount());
            if (CheckConditions(Hero.OneToOneConversationHero, Hero.MainHero, _cfg.values.ai.notableWillAcceptTheBribe))
            {
                persuade_status = persuade_type.bribe_success;
                TeachHeroTraits(Hero.OneToOneConversationHero, _cfg.values.peasantRevengerExcludeTrait, notable_can_do_revenge(Hero.OneToOneConversationHero));
            }
            else
            {
                persuade_status = persuade_type.bribe_fail;
            }
        }

        private bool peasant_revenge_player_not_happy_with_peasant_bribe_condition()
        {
            string msg = notable_can_do_revenge(Hero.OneToOneConversationHero) ?
                "{=PRev0118}Here take {BRIBEVALUE}{GOLD_ICON}. These noble people are not the criminals you are looking for." :
                "{=PRev0052}Here take {BRIBEVALUE}{GOLD_ICON}. Make them pay for their crimes!";

            TextObject textObject = new TextObject(msg, null);

            int bribe = get_notable_bribe_amount();

            textObject.SetTextVariable("BRIBEVALUE", bribe);

            MBTextManager.SetTextVariable("PAYER_COMMENT_REVENGE_BRIBE", textObject);

            return true;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_teach_condition()
        {
            string msg = "{=PRev0051}I can try to teach you by example.";

            //int honor = GetHeroTraitValue(Hero.OneToOneConversationHero, "Honor");
            //int generosity = GetHeroTraitValue(Hero.OneToOneConversationHero, "Generosity");

            if (notable_can_do_revenge(Hero.OneToOneConversationHero))
            {
                msg = "{=PRev0144}Great question! Let me demonstrate with an example...";
            }

            TextObject textObject = new TextObject(msg, null);
            MBTextManager.SetTextVariable("PAYER_COMMENT_REVENGE_TEACH", textObject);
            return true;
        }

        private bool notable_can_do_revenge(Hero hero)
        {
            return !hero_trait_list_condition(hero, _cfg.values.peasantRevengerExcludeTrait);
        }

        private bool peasant_revenge_player_not_happy_with_peasant_start_bribe_clickable(out TextObject text)
        {
            bool will_accept_bribe = CheckConditions(Hero.OneToOneConversationHero, Hero.MainHero, _cfg.values.ai.notableWillAcceptTheBribe);
            bool traits_allow = CheckOnlyTraitsConditions(Hero.OneToOneConversationHero, Hero.MainHero, _cfg.values.ai.notableWillAcceptTheBribe);
            bool have_gold = Hero.MainHero.Gold >= get_notable_bribe_amount();
            bool can_bribe = will_accept_bribe && have_gold;
            text = TextObject.Empty;

            if (!will_accept_bribe)
            {
                if (traits_allow)
                {
                    text = new TextObject("{=PRev0122}Your bribe will not work. Be more patient.");
                }
                else
                {
                    text = new TextObject("{=PRev0123}The Peasant's traits do not allow for bribes.");
                }
            }
            return have_gold && traits_allow && get_notable_persuaded_count() <=_cfg.values.lordCanTryAsManyTimesToPersuadeTheNotable;
        }
        private bool peasant_revenge_player_not_happy_with_peasant_start_teach_clickable(out TextObject text)
        {

            bool can_revenge_have_ex_traits = notable_can_do_revenge(Hero.OneToOneConversationHero) && hero_trait_list_condition(Hero.MainHero, _cfg.values.peasantRevengerExcludeTrait);
            bool cannot_revenge_have_no_ex_traits = !notable_can_do_revenge(Hero.OneToOneConversationHero) && !hero_trait_list_condition(Hero.MainHero, _cfg.values.peasantRevengerExcludeTrait);

            bool start = Hero.OneToOneConversationHero != null && (can_revenge_have_ex_traits||cannot_revenge_have_no_ex_traits) && get_notable_persuaded_count() <=_cfg.values.lordCanTryAsManyTimesToPersuadeTheNotable;

            text = TextObject.Empty;

            if (start)
            {
                text=new TextObject("{=PRev0145}Try to persuade the peasant");                
            }
            else
            {
                text = new TextObject("{=PRev0055}We are lacking the necessary traits");
            }

            return true;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_companion_take_notable_prisoner_clickable(out TextObject text)
        {
            text=new TextObject("{=PRev0146}Expell the peasant");
            return can_remove_notable_from_village();
        }


        private bool peasant_revenge_player_not_happy_with_peasant_execute_clickable(out TextObject text)
        {
            text = TextObject.Empty;
            text = new TextObject("{=PRev0147}Expell or execute the peasant");
            return true;
        }

        private void peasant_revenge_hero_not_happy_with_peasant_chop_consequence(Hero executioner_hero, Hero victim)
        {
            bool chop_purpose = notable_can_do_revenge(victim); // true if notable can do the revenge, but hero want to prohibit            

            foreach (Hero hero in victim.HomeSettlement.Notables)
            {
                int nobles_relations = hero.GetRelation(victim);// smaller the relation - bigger chance to get positive result towards player
                int hero_noble_relation = hero.GetRelation(executioner_hero); // bigger the relation - bigger chance to get positive result towards player 
                int relation_change = (hero_noble_relation > nobles_relations) ? _cfg.values.relationChangeWhenLordTeachPeasant : -_cfg.values.relationChangeWhenLordTeachPeasant;
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(executioner_hero, hero, relation_change, true);
                if (_cfg.values.enableOtherNobleTraitsChangeAfterNobleExecution)
                {

                    bool direction = MBRandom.RandomInt(-100, 100) < hero_noble_relation; // bigger relation means bigger chance direction is similar to chop purpose
                    TeachHeroTraits(hero, _cfg.values.peasantRevengerExcludeTrait, chop_purpose ? direction : !direction);
                }
            }
        }

        private void peasant_revenge_player_not_happy_with_peasant_chop_consequence()
        {
            peasant_revenge_hero_not_happy_with_peasant_chop_consequence(Hero.MainHero, Hero.OneToOneConversationHero);
            MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(Hero.MainHero, Hero.OneToOneConversationHero, SceneNotificationData.RelevantContextType.Map));
            KillCharacterAction.ApplyByExecution(Hero.OneToOneConversationHero, Hero.MainHero, true, false);
        }

        private void peasant_revenge_player_not_happy_with_peasant_companion_chop_consequence(Hero hero)
        {
            if (hero != null)
            {
                peasant_revenge_hero_not_happy_with_peasant_chop_consequence(hero, Hero.OneToOneConversationHero);
                MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(hero, Hero.OneToOneConversationHero, SceneNotificationData.RelevantContextType.Map));
                KillCharacterAction.ApplyByExecution(Hero.OneToOneConversationHero, hero, true, false);
            }
        }

       private void peasant_revenge_player_not_happy_with_peasant_companion_take_notable_prisoner_consequence()
        {
            KillCharacterAction.ApplyByRemove(Hero.OneToOneConversationHero,true,true);
        }

        private Hero get_first_companion() {
            TroopRoster troopsLordParty = MobileParty.MainParty.MemberRoster;
            for (int j = 0; j < troopsLordParty.Count; j++)
            {
                CharacterObject troop = troopsLordParty.GetCharacterAtIndex(j);
                if (troop.IsHero && !troop.IsPlayerCharacter)
                {
                    return troop.HeroObject;
                }
            }
            return null;
        }

        private bool peasant_revenge_get_executioner_companion_condition()
        {
            Hero companion = get_first_companion();
            if (companion != null)
            {
                StringHelpers.SetCharacterProperties("EXECUTIONER", companion.CharacterObject);
                return true;
            }
            return false;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_start_condition()
        {
            bool start = Hero.OneToOneConversationHero != null &&
                (Hero.OneToOneConversationHero.IsHeadman || Hero.OneToOneConversationHero.IsRuralNotable) &&
                Hero.OneToOneConversationHero.Issue == null;
            if (start)
            {
                previous_can_revenge = notable_can_do_revenge(Hero.OneToOneConversationHero); // true if notable can do the revenge, but hero want to prohibit
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

            TextObject textObject = new TextObject("{=PRev0096}{RANSOMER.LINK} offers you {GOLD_AMOUNT}{GOLD_ICON} in ransom for {CAPTIVE_HERO.NAME}.", null);
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
                        new TextObject("{=Y94H6XnK}Accept", null).ToString(),
                        new TextObject("{=cOgmdp9e}Decline", null).ToString(),
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
                textObject = new TextObject("{=PRev0097}Nobody wants to pay for {CAPTIVE_HERO.NAME}");
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

                if (revenge != null) // have revenge data with peasant, who cannot start dialog (not started quest)
                {
                    retval = true;
                    //setting currentRevenge because dialogue need to know whitch revenge is talking about 
                    currentRevenge = revenge;
                    create_peasant_comment_at_revenge_start(currentRevenge);
                }
                else
                {
                    //it is with different state or has no revengeData
                }

                return retval;
            }
            else
            {
                return false;
            }
        }

        private bool peasant_revenge_revenger_start_no_quest_data_condition()
        {
            if(Hero.OneToOneConversationHero==null)
                return false;

            if((Hero.OneToOneConversationHero.IsHeadman||Hero.OneToOneConversationHero.IsRuralNotable)&&
                     Hero.OneToOneConversationHero.PartyBelongedTo!=null&&
                     Hero.OneToOneConversationHero.PartyBelongedTo.StringId.StartsWith(revengerPartyNameStart))
            {
                //Here we have only revenger party
                PeasantRevengeData revenge = revengeData.FirstOrDefault((x) =>
                x.executioner!=null &&
                x.executioner.HeroObject==Hero.OneToOneConversationHero);
                return revenge == null;
            }
            else
            {
                return false;
            }
        }

        private void create_peasant_comment_at_revenge_start(PeasantRevengeData revenge)
        {
            string msg = "{=PRev0078}I do not have time to talk right now.[rf:idle_angry][ib:closed][if:idle_angry]";

            int honor = GetHeroTraitValue(revenge.executioner.HeroObject, "Honor");
            int generosity = GetHeroTraitValue(revenge.executioner.HeroObject, "Generosity");

            if (honor > 0)
            {
                msg ="{=PRev0114}Nobody can stop me from taking revenge on {CRIMINAL.LINK}![rf:idle_angry][if:convo_furious][ib:angry]";
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
                //Find, if peasant cannot start other dialogs ( status == begin )
                PeasantRevengeData revenge = revengeData.FirstOrDefault((x) =>
                x.executioner != null &&
                x.executioner.HeroObject == Hero.OneToOneConversationHero &&
                // x.targetHero != Hero.MainHero.CharacterObject &&
                x.state > PeasantRevengeData.quest_state.start);

                bool retval = false;

                if (revenge != null) // have revenge data with peasant, who cannot start dialog (finished quest)
                {
                    retval = true;
                    //setting currentRevenge because dialogue need to know whitch revenge is talking about 
                    currentRevenge = revenge;
                    create_peasant_comment_at_revenge_end(currentRevenge);
                }
                else
                {
                    //it is with different state or has no revengeData
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
            string msg = "{=PRev0078}I do not have time to talk right now.[rf:idle_angry][ib:closed][if:idle_angry]";
            TextObject textObject;

            if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed) &&
            currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed))
            {
                msg ="{=PRev0111}{CRIMINAL.NAME} and {CVICTIM.LINK} are dead! My revenge has been achieved![if:happy]";
                textObject = new TextObject(msg,null);
                if(currentRevenge.accused_hero!=null)
                {
                    StringHelpers.SetCharacterProperties("CVICTIM",currentRevenge.accused_hero,textObject,false);
                }

                if(revenge.criminal!=null)
                {
                    StringHelpers.SetCharacterProperties("CRIMINAL",revenge.criminal,textObject,false);
                }
            }
            else
            {
                if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.accused_hero_killed))
                {
                    msg ="{=PRev0112}I got my revenge on {CVICTIM.LINK}![if:happy]";
                    textObject = new TextObject(msg,null);
                    if(currentRevenge.accused_hero!=null)
                    {
                        StringHelpers.SetCharacterProperties("CVICTIM",currentRevenge.accused_hero,textObject,false);
                    }
                }
                else if (currentRevenge.quest_Results.Contains(PeasantRevengeData.quest_result.criminal_killed))
                {
                    msg ="{=PRev0112}I got my revenge on {CVICTIM.LINK}![if:happy]";
                    textObject=new TextObject(msg,null);
                    if(revenge.criminal!=null)
                    {
                        StringHelpers.SetCharacterProperties("CVICTIM",revenge.criminal,textObject,false);
                    }
                }
                else
                {
                    textObject = new TextObject(msg,null);
                }
            }

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
                string LogMessage = "{=PRev0040}{PARTYOWNER.NAME} decided not to execute {PRISONER.NAME} after {SAVER.NAME} paid {REPARATION}{GOLD_ICON} in reparation.";
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
                textObject = new TextObject("{=PRev0088}Disable the mobile notable peasant party");
            }
            else
            {
                textObject = new TextObject("{=PRev0089}Enable the mobile notable peasant party");
            }

            return true;
        }
        private bool peasant_revenge_enable_neutral_village_attack_clickable_condition(out TextObject textObject)
        {
            if (_cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu)
            {
                textObject = new TextObject("{=PRev0090}Disable the option to defend the village against neutral mobile parties");
            }
            else
            {
                textObject = new TextObject("{=PRev0091}Enable the option to defend the village against neutral mobile parties");
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

        private void leave_encounter_and_mission()
        {
            if(PlayerEncounter.Current==null)
                return;
            PlayerEncounter.LeaveEncounter=true;
            if(PlayerEncounter.InsideSettlement)
                if(CampaignMission.Current != null)
                    CampaignMission.Current.EndMission();
            if(currentRevenge.xParty!=null)
                currentRevenge.xParty.Ai.SetMoveModeHold();
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
                text = new TextObject("{=PRev0021}{PARTYLEADER.LINK} caught {CRIMINAL.LINK} looting our village. We demand the criminal's head to be impaled on a spike. This bastard must pay for their crime in blood![ib:aggressive][if:convo_furious]");
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
