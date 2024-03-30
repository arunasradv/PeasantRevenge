using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace PeasantRevenge
{
    internal class HelpNeutralVillageBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this,OnGameLoadedEvent);
        }

        public override void SyncData(IDataStore dataStore)
        {
          
        }

        private void OnGameLoadedEvent(CampaignGameStarter campaignGameStarter)
        {
            AddGameMenus(campaignGameStarter);
        }

        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "join_encounter",
                "join_encounter_help_defenders_force",
                "{=PRev0087}Declare war to {KINGDOM}, and help {DEFENDER}.",
                new GameMenuOption.OnConditionDelegate(this.game_menu_join_encounter_help_defenders_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_encounter_help_defenders_on_consequence),
                false,-1,false,null);
        }

        private bool game_menu_join_encounter_help_defenders_on_condition(MenuCallbackArgs args)
        {
            bool enableHelpNeutralVillageAndDeclareWarToAttackerMenu = true; // TODO: Add this into configuration.

            if(!enableHelpNeutralVillageAndDeclareWarToAttackerMenu)
                return false;

            args.optionLeaveType=GameMenuOption.LeaveType.DefendAction;
            MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
            IFaction mapFactionAttacker = encounteredBattle.GetLeaderParty(BattleSideEnum.Attacker).MapFaction;
            //IFaction mapFactionDefender = encounteredBattle.GetLeaderParty(BattleSideEnum.Defender).MapFaction;

            bool canStartHelpVillageMenu = encounteredBattle.MapEventSettlement!=null&&
                !mapFactionAttacker.IsAtWarWith(MobileParty.MainParty.MapFaction)&&
                //!mapFactionDefender.IsAtWarWith(MobileParty.MainParty.MapFaction) &&
                mapFactionAttacker!=MobileParty.MainParty.MapFaction&& // if removed can attack own party (not for this mod)
                encounteredBattle.MapEventSettlement.IsVillage&&
                encounteredBattle.MapEventSettlement.IsUnderRaid;

            if(canStartHelpVillageMenu)
            {
                MBTextManager.SetTextVariable("KINGDOM",mapFactionAttacker.Name.ToString());
                if(mapFactionAttacker.NotAttackableByPlayerUntilTime.IsFuture)
                {
                    args.IsEnabled=false;
                    args.Tooltip=GameTexts.FindText("str_enemy_not_attackable_tooltip",null);
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

            if(!mapFactionAttacker.IsAtWarWith(MobileParty.MainParty.MapFaction))
            {
                BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty,encounteredBattle.GetLeaderParty(BattleSideEnum.Attacker));
                //if (MobileParty.MainParty.MapFaction == mapFactionAttacker)
                //{
                //    ChangeCrimeRatingAction.Apply(MobileParty.MainParty.MapFaction, 61f);
                //}
            }

            if(((encounteredParty!=null) ? encounteredParty.MapEvent : null)!=null)
            {
                PlayerEncounter.JoinBattle(BattleSideEnum.Defender);
                GameMenu.ActivateGameMenu("encounter");
                if(!mapFactionDefender.IsAtWarWith(MobileParty.MainParty.MapFaction))
                {
                    TextObject menuText = new TextObject("{=PRev0086}You decided to...");
                    MBTextManager.SetTextVariable("ENCOUNTER_TEXT",menuText,true);
                }
                return;
            }
        }
    }
}
