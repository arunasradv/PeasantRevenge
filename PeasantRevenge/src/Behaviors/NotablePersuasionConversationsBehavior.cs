
using Helpers;
using System;
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
using static PeasantRevenge.Common;

namespace PeasantRevenge
{
#pragma warning disable IDE1006 // Naming Styles
    public class NotablePersuasionConversationsBehavior : CampaignBehaviorBase
    {
        public override void SyncData(IDataStore dataStore)
        {

        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoadedEvent);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreatedEvent);
        }

        private void OnNewGameCreatedEvent(CampaignGameStarter campaignGameStarter)
        {
            AddDialogs(campaignGameStarter);
        }

        private void OnGameLoadedEvent(CampaignGameStarter campaignGameStarter)
        {
            AddDialogs(campaignGameStarter);
        }

        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            AddPeasantPersuasionDialogs(campaignGameStarter);
            campaignGameStarter.AddDialogFlow(NotablePersuasionDialogFlow());
        }

        PersuasionTask _task;
        event_status persuade_status = event_status.none;
        public class PersuadedHeroData
        {

            public string Id = ""; // hero id
            public uint persuade_try_count = 0;
        }
        public List<PersuadedHeroData> persuadedHeroData = new List<PersuadedHeroData>();

        bool previous_can_revenge = false;

        private void AddPeasantPersuasionDialogs(CampaignGameStarter campaignGameStarter)
        {
            #region Peasant revenge configuration via dialog
            /*
                        campaignGameStarter.AddDialogLine(
                                      "peasant_revenge_player_config_mod_npc_options",
                                      "peasant_revenge_player_config_mod_options_set",
                                      "peasant_revenge_player_config_mod_options_set",
                                      "{=PRev0084}Yes, my {?MAINHERO.GENDER}Lady{?}Lord{\\?}.[rf:convo_thinking]", null, null, 200, null);
                        campaignGameStarter.AddPlayerLine(
                           "peasant_revenge_player_config_mod_option_mp_dis",
                           "peasant_revenge_player_not_happy_with_peasant_start_options",
                           "peasant_revenge_player_config_mod_end_dis",
                           "{=PRev0081}You should not immediately interrupt me with any of your matters.",
                            () => { return !_cfg.values.enableRevengerMobileParty; }, () => { SetEnableRevengerMobileParty(true); },
                            110,
                            new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_party_clickable_condition));
                        campaignGameStarter.AddPlayerLine(
                           "peasant_revenge_player_config_mod_option_mp_en",
                           "peasant_revenge_player_not_happy_with_peasant_start_options",
                           "peasant_revenge_player_config_mod_end_en",
                           "{=PRev0082}You should immediately interrupt me with any of your matters.",
                            () => { return _cfg.values.enableRevengerMobileParty; }, () => { SetEnableRevengerMobileParty(false); },
                            110,
                            new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_party_clickable_condition));
                        campaignGameStarter.AddPlayerLine(
                          "peasant_revenge_player_config_mod_option_np_en",
                          "peasant_revenge_player_not_happy_with_peasant_start_options",
                          "peasant_revenge_player_config_mod_end_dis",
                          "{=PRev0092}I will defend villages from any looters.",
                           () => { return !_cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu; }, () => { SetEnableHelpNeutralVillage(true); }, 100,
                           new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_neutral_village_attack_clickable_condition));
                        campaignGameStarter.AddPlayerLine(
                          "peasant_revenge_player_config_mod_option_np_dis",
                          "peasant_revenge_player_not_happy_with_peasant_start_options",
                          "peasant_revenge_player_config_mod_end_en",
                          "{=PRev0093}I will defend villages from my enemies only.",
                           () => { return _cfg.values.enableHelpNeutralVillageAndDeclareWarToAttackerMenu; }, () => { SetEnableHelpNeutralVillage(false); }, 100,
                           new ConversationSentence.OnClickableConditionDelegate(peasant_revenge_enable_neutral_village_attack_clickable_condition));

                        campaignGameStarter.AddDialogLine(
                         "peasant_revenge_player_config_mod_npc_end_dis",
                         "peasant_revenge_player_config_mod_end_dis",
                         "peasant_revenge_player_not_happy_with_peasant_start_options",
                         "{=PRev0084}Yes, my {?MAINHERO.GENDER}Lady{?}Lord{\\?}.[rf:idle_happy]",
                         () => { StringHelpers.SetCharacterProperties("MAINHERO", Hero.MainHero.CharacterObject); return true; }, null, 200, null);
                        campaignGameStarter.AddDialogLine(
                         "peasant_revenge_player_config_mod_npc_end_en",
                         "peasant_revenge_player_config_mod_end_en",
                         "peasant_revenge_player_not_happy_with_peasant_start_options",
                         "{=PRev0084}Yes, my {?MAINHERO.GENDER}Lady{?}Lord{\\?}.[rf:idle_angry][ib:closed]",
                         () => { StringHelpers.SetCharacterProperties("MAINHERO", Hero.MainHero.CharacterObject); return true; }, null, 200, null);
            */
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
              () =>
              {
                  peasant_revenge_player_not_happy_with_peasant_teaching_consequence();
#warning TODO Check if notable has active revenge quest.
                  //StopRevengeForNotableIfAny(Hero.OneToOneConversationHero);
                  leave_encounter();
              },
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
            /*TODO: Companion trait developement - maybe persuade by specfic reason with different traits*/
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
              , 110,
              new ConversationSentence.OnClickableConditionDelegate(this.peasant_revenge_player_not_happy_with_peasant_companion_take_notable_prisoner_clickable));
            campaignGameStarter.AddPlayerLine(
             "peasant_revenge_player_not_happy_with_peasant_end_accusation_exit",
             "peasant_revenge_player_not_happy_with_peasant_end_accusation_options",
             "close_window",
             "{=PRev0094}I must leave now.", null, () => leave_encounter(),
             0, null);
            //TEACH
            #endregion
            #endregion
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

        private void peasant_revenge_player_not_happy_with_peasant_bribe_consequence()
        {
            /*add_notable_persuaded_count();*/
            /*Makes more sense, since huge amount of bribes should work*/
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, get_notable_bribe_amount(Hero.OneToOneConversationHero));
            if (CheckConditions(Hero.OneToOneConversationHero, Hero.MainHero, _cfg.values.ai.notableWillAcceptTheBribe))
            {
                persuade_status = event_status.bribe_success;
                TeachHeroTraits(Hero.OneToOneConversationHero, _cfg.values.peasantRevengerExcludeTrait, notable_can_do_revenge(Hero.OneToOneConversationHero));
            }
            else
            {
                persuade_status = event_status.bribe_fail;
            }
        }

        private bool peasant_revenge_player_not_happy_with_peasant_bribe_condition()
        {
            string msg = notable_can_do_revenge(Hero.OneToOneConversationHero) ?
                "{=PRev0118}Here take {BRIBEVALUE}{GOLD_ICON}. These noble people are not the criminals you are looking for." :
                "{=PRev0052}Here take {BRIBEVALUE}{GOLD_ICON}. Make them pay for their crimes!";

            TextObject textObject = new TextObject(msg, null);

            int bribe = get_notable_bribe_amount(Hero.OneToOneConversationHero);

            textObject.SetTextVariable("BRIBEVALUE", bribe);

            MBTextManager.SetTextVariable("PAYER_COMMENT_REVENGE_BRIBE", textObject);

            return true;
        }


        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_can_revenge_on_condition()
        {
            if (!previous_can_revenge &&
            (persuade_status == event_status.bribe_success || persuade_status == event_status.show_example_success))
            {
                return true;
            }
            return false;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_not_revenge_on_condition()
        {
            if (previous_can_revenge && (persuade_status == event_status.bribe_success || persuade_status == event_status.show_example_success))
            {
                return true;
            }
            return false;
        }


        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_fail_on_condition()
        {
            if (persuade_status == event_status.bribe_fail || persuade_status == event_status.show_example_fail)
            {
                TextObject textObject = new TextObject("{=*}{COMMENT_LINE}", null);
                textObject.SetTextVariable("COMMENT_LINE", new TextObject("{=PRev0121}I can't make any promises..[ib:closed]", null));
                MBTextManager.SetTextVariable("FAIL_PERSUADE_LINE", textObject, false);
                return true;
            }

            if (persuade_status == event_status.accusation_fail)
            {
                TextObject textObject = new TextObject("{=*}{COMMENT_LINE}", null);
                textObject.SetTextVariable("COMMENT_LINE", new TextObject("{=PRev0143}Let's say it was a misunderstanding.", null));
                MBTextManager.SetTextVariable("FAIL_PERSUADE_LINE", textObject, false);
                return true;
            }

            return false;
        }


        private void peasant_revenge_player_not_happy_with_peasant_teaching_consequence()
        {
            if (persuade_status == event_status.show_example_fail || persuade_status == event_status.show_example_success)
            {
                if (previous_can_revenge)
                {
                    OnLordPersuedeNotableNotToRevenge(Hero.MainHero);
                }
                else
                {
                    OnLordPersuedeNotableToRevenge(Hero.MainHero);
                }

                if (persuade_status == event_status.show_example_success)
                {
                    TeachHeroTraits(Hero.OneToOneConversationHero, _cfg.values.peasantRevengerExcludeTrait, previous_can_revenge);
                }
            }

            if (persuade_status == event_status.bribe_fail || persuade_status == event_status.bribe_success)
            {
                //TODO: make notable traits depended negative outcome
                int relation_change = //persuade_status == persuade_type.bribe_fail ? -_cfg.values.relationChangeWhenLordBribePeasant :
               _cfg.values.relationChangeWhenLordBribePeasant;

                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, Hero.MainHero,
                relation_change,
                _cfg.values.relationChangeWhenLordBribePeasant != 0);
            }
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_accusation_success_on_condition()
        {
            return persuade_status == event_status.accusation_success;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_end_accusation_companion_clickable(out TextObject explanation)
        {
            explanation = null;
            return can_remove_notable_from_village_on_conversation();
        }
        private bool peasant_revenge_player_not_happy_with_peasant_end_accusation_clickable(out TextObject explanation)
        {
            explanation = null;
            return can_remove_notable_from_village_on_conversation();
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

        private bool peasant_revenge_player_not_happy_with_peasant_execute_clickable(out TextObject text)
        {
            text = TextObject.GetEmpty();
            text = new TextObject("{=PRev0147}Expell or execute the peasant");
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

        private bool peasant_revenge_player_not_happy_with_peasant_start_bribe_clickable(out TextObject text)
        {
            bool will_accept_bribe = CheckConditions(Hero.OneToOneConversationHero, Hero.MainHero, _cfg.values.ai.notableWillAcceptTheBribe);
            bool traits_allow = CheckOnlyTraitsConditions(Hero.OneToOneConversationHero, Hero.MainHero, _cfg.values.ai.notableWillAcceptTheBribe);
            bool have_gold = Hero.MainHero.Gold >= get_notable_bribe_amount(Hero.OneToOneConversationHero);
            bool can_bribe = will_accept_bribe && have_gold;
            text = TextObject.GetEmpty();

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
            return have_gold && traits_allow && get_notable_persuaded_count() <= _cfg.values.lordCanTryAsManyTimesToPersuadeTheNotable;
        }
        private bool peasant_revenge_player_not_happy_with_peasant_start_teach_clickable(out TextObject text)
        {

            bool can_revenge_have_ex_traits = notable_can_do_revenge(Hero.OneToOneConversationHero) && CfgParser.hero_trait_list_condition(Hero.MainHero, _cfg.values.peasantRevengerExcludeTrait, out string parseerror);
            bool cannot_revenge_have_no_ex_traits = !notable_can_do_revenge(Hero.OneToOneConversationHero) && !CfgParser.hero_trait_list_condition(Hero.MainHero, _cfg.values.peasantRevengerExcludeTrait, out parseerror);

            bool start = Hero.OneToOneConversationHero != null && (can_revenge_have_ex_traits || cannot_revenge_have_no_ex_traits) && get_notable_persuaded_count() <= _cfg.values.lordCanTryAsManyTimesToPersuadeTheNotable;

            text = TextObject.GetEmpty();

            if (start)
            {
                text = new TextObject("{=PRev0145}Try to persuade the peasant");
            }
            else
            {
                text = new TextObject("{=PRev0055}We are lacking the necessary traits");
            }

            return true;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_companion_take_notable_prisoner_clickable(out TextObject text)
        {
            text = new TextObject("{=PRev0146}Expell the peasant");
            return can_remove_notable_from_village_on_conversation();
        }

        private void peasant_revenge_player_not_happy_with_peasant_chop_consequence()
        {
            OnLordExecuteRevengerAfterOrBeforeQuest(Hero.MainHero);
            OnHeroChopNotableHeadConsequence(Hero.MainHero, Hero.OneToOneConversationHero);
            MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(Hero.MainHero, Hero.OneToOneConversationHero, SceneNotificationData.RelevantContextType.Map));
            KillCharacterAction.ApplyByExecution(Hero.OneToOneConversationHero, Hero.MainHero, true, false);
        }

        private void peasant_revenge_player_not_happy_with_peasant_companion_chop_consequence(Hero hero)
        {
            if (hero != null)
            {
                OnLordExecuteRevengerAfterOrBeforeQuest(hero);
                OnHeroChopNotableHeadConsequence(hero, Hero.OneToOneConversationHero);
                MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(hero, Hero.OneToOneConversationHero, SceneNotificationData.RelevantContextType.Map));
                KillCharacterAction.ApplyByExecution(Hero.OneToOneConversationHero, hero, true, false);

            }
        }

        private void peasant_revenge_player_not_happy_with_peasant_companion_take_notable_prisoner_consequence()
        {
            KillCharacterAction.ApplyByRemove(Hero.OneToOneConversationHero, true, true);
        }

        private Hero get_first_companion()
        {
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

        private DialogFlow NotablePersuasionDialogFlow()
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
               () =>
               {
                   this.peasant_revenge_persuasion_rejected_on_consequence();
                   leave_encounter_and_mission();
               },
               this, 100, null,
               null,
               null);

            dialog.AddDialogLine(
                "peasant_revenge_persuasion_failed",
                "peasant_revenge_persuasion_start_reservation",
                "peasant_revenge_player_not_happy_with_peasant_post_learned",
                "{=!}{FAILED_PERSUASION_LINE}",
                new ConversationSentence.OnConditionDelegate(this.peasant_revenge_persuasion_failed_on_condition),
                new ConversationSentence.OnConsequenceDelegate(this.peasant_revenge_persuasion_failed_on_consequence),
                this, 100, null,
                null,
               null);

            dialog.AddDialogLine(
                "peasant_revenge_persuasion_success",
                "peasant_revenge_persuasion_start_reservation",
                "peasant_revenge_player_not_happy_with_peasant_post_learned",
                "{=PRev0128}You're right.",
                new ConversationSentence.OnConditionDelegate(ConversationManager.GetPersuasionProgressSatisfied),
                new ConversationSentence.OnConsequenceDelegate(this.peasant_revenge_persuasion_success_on_consequence),
                this, int.MaxValue, null,
                null,
               null);

            dialog.AddDialogLine("peasant_revenge_persuasion_attempt",
                "peasant_revenge_persuasion_start_reservation",
                "peasant_revenge_persuasion_select_option",
                "{=PRev0129}What's there to discuss?",
                () => { return persuade_not_failed_on_condition(); },
                null, this, 10, null, null, null);


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
                     null,
               null);
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
                     null,
               null);
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
                    null,
               null);
            dialog.AddPlayerLine(
                    "peasant_revenge_persuasion_select_option_cancel",
                    "peasant_revenge_persuasion_select_option",
                    "peasant_revenge_persuasion_start_reservation",
                    "{=PRev0094}I must leave now.",
                    () => { return true; },
                    () => { persuasion_cancel_on_consequence(); },
                    this, 100, null, null,
                    null,
               null);

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



        private bool persuasion_start_with_notable_on_condition()
        {
            if (this._task.Options.Count > 0)
            {
                TextObject textObject = new TextObject("{=*}{COMMENT_LINE}", null);

                if (persuade_status == event_status.accusation)
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

        private void persuasion_start_with_notable_on_consequence()
        {
            if (persuade_status == event_status.accusation)
            {

                ConversationManager.StartPersuasion(1f, 1f, 0f, 1f, 1f, 0f, PersuasionDifficulty.Hard);
            }
            else
            {
                ConversationManager.StartPersuasion(1f, 1f, 0f, 1f, 1f, 0f, PersuasionDifficulty.Hard);
            }
        }

        private void peasant_revenge_player_not_happy_with_peasant_teach_consequence()
        {
            bool can_revenge = notable_can_do_revenge(Hero.OneToOneConversationHero);
            int task_index = 0;

            if (can_revenge)
            {
                persuade_status = event_status.teach_to_not_revenge;
            }
            else
            {
                persuade_status = event_status.teach_to_revenge;
            }

            task_index = GetTaskIndexByPersuadeStatus(persuade_status);
            _task = GetPersuasionTask(task_index);
            _task.UnblockAllOptions();

            add_notable_persuaded_count();
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

        private void peasant_revenge_player_not_happy_with_peasant_accuse_consequence()
        {
            persuade_status = event_status.accusation;
            _task = GetPersuasionTask(2);
            _task.UnblockAllOptions();
        }

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

        private void persuasion_cancel_on_consequence()
        {
            if (this._task.Options.Count > 0)
            {
                this._task.BlockAllOptions();
            }
        }

        private void persuasion_select_option_i_on_consequence(int option_index)
        {
            if (this._task.Options.Count > 0)
            {
                int task_index = GetTaskIndexByPersuadeStatus(persuade_status);
                var traits_values = GetTraitsAndValuesByTaskAndOption(task_index, option_index);
                OnLordUseTraitsAndValues(Hero.MainHero, traits_values);

                this._task.BlockAllOptions();
                // no need to allow select different option, because player is developing hero in one way.
                //if (persuade_status == persuade_type.accusation)
                //{
                //    this._task.BlockAllOptions();
                //}
                //else
                //{
                //    this._task.Options[option_index].BlockTheOption(true);
                //}
            }
        }

        private bool persuasion_clickable_option_i_on_condition(int option_index, out TextObject hintText)
        {
            hintText = TextObject.GetEmpty();

            if (this._task.Options.Count > 0)
            {
                string s = Common.GetInfoStringForTraitsAndValues(GetTraitsAndValuesByTaskAndOption(
                    GetTaskIndexByPersuadeStatus(persuade_status), option_index));
                TextObject traitHintText = new TextObject(s, null);
                TextObject blockedHintText = new TextObject("{=9ACJsI6S}Blocked", null);
                hintText = this._task.Options.ElementAt(option_index).IsBlocked ? blockedHintText : traitHintText;
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

        private uint get_notable_persuaded_count()
        {
            if (Hero.OneToOneConversationHero == null) return 0;

            var pdata = persuadedHeroData.Where((x) => x.Id.Equals(Hero.OneToOneConversationHero.StringId)).FirstOrDefault();

            return pdata != null ? pdata.persuade_try_count : 0;
        }

        private bool peasant_revenge_player_not_happy_with_peasant_post_learned_refuse_on_condition()
        {
            return get_notable_persuaded_count() > _cfg.values.lordCanTryAsManyTimesToPersuadeTheNotable;
        }

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
            if (persuade_status == event_status.accusation)
            {
                persuade_status = event_status.accusation_fail;
            }
            else
            {
                persuade_status = event_status.show_example_fail;
            }
        }

        private void peasant_revenge_persuasion_failed_on_consequence()
        {
            ConversationManager.EndPersuasion();
            if (persuade_status == event_status.accusation)
            {
                persuade_status = event_status.accusation_fail;
            }
            else
            {
                persuade_status = event_status.show_example_fail;
            }
        }

        private void peasant_revenge_persuasion_success_on_consequence()
        {
            ConversationManager.EndPersuasion();
            if (persuade_status == event_status.accusation)
            {
                persuade_status = event_status.accusation_success;
            }
            else
            {
                persuade_status = event_status.show_example_success;
            }
        }

        private void leave_encounter()
        {
            if (PlayerEncounter.Current == null) return;
            PlayerEncounter.LeaveEncounter = true;
            //currentRevenge.xParty?.SetMoveModeHold();
        }

        private void leave_encounter_and_mission()
        {
            if (PlayerEncounter.Current == null)
                return;
            PlayerEncounter.LeaveEncounter = true;
            if (PlayerEncounter.InsideSettlement)
                CampaignMission.Current?.EndMission();
            //currentRevenge.xParty?.SetMoveModeHold();
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
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject, GetTraitsAndValuesByTaskAndOption(task_index, 0)),
                    false, new TextObject("{=PRev0132}No one should be afraid of these criminals.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option0);
                PersuasionOptionArgs option1 = new PersuasionOptionArgs(DefaultSkills.Engineering, DefaultTraits.Mercy, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject, GetTraitsAndValuesByTaskAndOption(task_index, 1)),
                    false, new TextObject("{=PRev0133}Someone must be held accountable for the destruction of our village!", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option1);
                PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Negative,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject, GetTraitsAndValuesByTaskAndOption(task_index, 2)),
                    false, new TextObject("{=PRev0134}Take justice into your own hands!", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option2);
            }
            else if (task_index == 1)
            {
                PersuasionOptionArgs option0 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Valor, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject, GetTraitsAndValuesByTaskAndOption(task_index, 0)),
                    false, new TextObject("{=PRev0135}These criminals are too dangerous.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option0);
                PersuasionOptionArgs option1 = new PersuasionOptionArgs(DefaultSkills.Engineering, DefaultTraits.Mercy, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject, GetTraitsAndValuesByTaskAndOption(task_index, 1)),
                    false, new TextObject("{=PRev0136}Pity for your enemy is cruelty onto your ally.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option1);
                PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive,
                     GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject, GetTraitsAndValuesByTaskAndOption(task_index, 2)),
                    false, new TextObject("{=PRev0137}Let the nobles take care of the judgement. You are not important enough.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option2);
            }
            else if (task_index == 2)
            {
                PersuasionOptionArgs option0 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject, GetTraitsAndValuesByTaskAndOption(task_index, 0)),
                    false, new TextObject("{=PRev0138}Everyone has heard of your hostile speeches against nobles.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option0);
                PersuasionOptionArgs option1 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Mercy, TraitEffect.Negative,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject, GetTraitsAndValuesByTaskAndOption(task_index, 1)),
                    false, new TextObject("{=PRev0139}Your kindness to the enemy is harmful enough to consider it criminal.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option1);
                PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive,
                    GetPersuationArgumentStrength(Hero.OneToOneConversationHero.CharacterObject, GetTraitsAndValuesByTaskAndOption(task_index, 2)),
                    false, new TextObject("{=PRev0140}Everyone knows I'm telling the truth.", null), null, false, false, false);
                persuasionTask.AddOptionToTask(option2);
            }

            return persuasionTask;
        }

        private Tuple<TraitObject, int>[] GetTraitCorrelations(int valor = 0, int mercy = 0, int honor = 0, int generosity = 0, int calculating = 0)
        {
            return new Tuple<TraitObject, int>[]
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

            foreach (PeasantRevengeConfiguration.TraitAndValue tv in traits_and_values)
            {
                switch (tv.trait)
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

            Tuple<TraitObject, int>[] traitCorrelations = this.GetTraitCorrelations(valor, mercy, honor, generosity, calculating);
            PersuasionArgumentStrength argstr = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(target_hero, traitCorrelations); // how much argument trait tuple correlates with npc and player  

            PersuasionDifficulty min_difficulty = PersuasionDifficulty.Medium;

            PersuasionDifficulty difficulty = GetStartPersuasionDifficulty(Hero.MainHero, target_hero.HeroObject, min_difficulty);

            argstr = argstr - (difficulty - min_difficulty);

            if (argstr < PersuasionArgumentStrength.ExtremelyHard)
            {
                argstr = PersuasionArgumentStrength.ExtremelyHard;
            }
            else if (argstr > PersuasionArgumentStrength.ExtremelyEasy)
            {
                argstr = PersuasionArgumentStrength.ExtremelyEasy;
            }

            return argstr;
        }

        private PersuasionDifficulty GetStartPersuasionDifficulty(Hero hero_initiator, Hero hero_target, PersuasionDifficulty min_difficulty)
        {
            PersuasionDifficulty diff = min_difficulty;

            bool can_revenge = notable_can_do_revenge(hero_target);
            bool main_have_exclude_trait = CfgParser.hero_trait_list_condition(hero_initiator, _cfg.values.peasantRevengerExcludeTrait, out string parseerror);
            bool can_revenge_have_ex_traits = can_revenge && main_have_exclude_trait;
            bool cannot_revenge_have_no_ex_traits = !can_revenge && !main_have_exclude_trait;
            bool have_traits = can_revenge_have_ex_traits || cannot_revenge_have_no_ex_traits;

            if (!have_traits)
            {
                diff += 1;
            }

            if (hero_initiator.MapFaction != hero_target.MapFaction)
            {
                diff += 1;
            }

            if (hero_initiator.MapFaction.IsAtWarWith(hero_target.MapFaction))
            {
                diff += 1;
            }

            int relation = hero_target.GetRelation(hero_initiator);

            if (relation < 0)
            {
                diff += 1;
            }
            else if (relation > 20)
            {
                diff -= 1;
            }

            if (hero_target.IsEnemy(hero_initiator))
            {
                diff += 1;
            }
            else if (hero_target.IsFriend(hero_initiator))
            {
                diff -= 1;
            }

            if (diff > PersuasionDifficulty.Impossible)
            {
                diff = PersuasionDifficulty.Impossible;
            }

            return diff;
        }

    }
}