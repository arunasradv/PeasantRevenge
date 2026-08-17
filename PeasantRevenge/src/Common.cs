using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace PeasantRevenge
{
    public static class Common
    {
        public static PeasantRevengeModCfg _cfg;

        public static List<string> emotion_list = new List<string> {
            "[if:convo_normal]",
            "[if:convo_nonchalant]",
            "[if:convo_pondering]",
            "[if:convo_thinking]",
            "[if:convo_contemptuous]",
            "[ib:closed]",
            "[if:convo_bemused]",
            "[ib:normal][if:convo_calm_friendly]",
            "[ib:confident2][if:convo_calm_friendly]",
            "[ib:normal][if:convo_relaxed_happy]",
            "[if:convo_happy]",
            "[if:convo_focused_happy]",
            "[if:convo_evil_smile]",
            "[if:convo_merry]",
            "[if:convo_huge_smile]",
            "[if:convo_excited]",
            "[if:convo_approving]",
            "[if:convo_shocked]",
            "[if:convo_astonished]",
            "[if:convo_disbelief]",
            "[if:convo_confused_normal]",
            "[if:convo_confused_annoyed]",
            "[ib:hip][if:convo_uncomfortable_voice]",
            "[if:convo_worried]",
            "[ib:closed][if:convo_worried]",
            "[if:convo_nervous]",
            "[ib:nervous][if:convo_bared_teeth]",
            "[if:convo_nervous2]",
            "[if:convo_annoyed]",
            "[if:convo_annoyed][ib:warrior2]",
            "[if:convo_insulted]",
            "[if:convo_contemptuous]",
            "[if:convo_mocking_teasing]",
            "[if:convo_mocking_aristocratic]",
            "[if:convo_mocking_revenge]",
            "[if:convo_very_stern]",
            "[if:convo_stern]",
            "[if:convo_grave]",
            "[ib:closed2][if:convo_grave]",
            "[if:convo_aggressive]",
            "[ib:aggressive][if:convo_bared_teeth]",
            "[if:convo_furious]",
            "[if:convo_undecided_open]",
            "[ib:demure][if:convo_undecided_open]",
            "[ib:confident][if:convo_undecided_closed]",
            "[ib:closed][if:convo_nervous]",
            "[ib:nervous][if:convo_nervous2]",
            "[ib:hip2][if:convo_huge_smile]",
            "[ib:warrior][if:convo_furious]"
            };

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
            accusation_success,
            expeled,
            killed,

            village_denied,
            party_denied,
            clan_denied,
            kingdom_denied,
            party_no_decision,
            clan_leader_no_decision,
            saver_no_decision,
            notable_interrupted,
            accusation_fail_both_blamed,
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

        private static bool IsModuleVersionOlder(ApplicationVersion module_version, ApplicationVersion compare)
        {
            bool is_older = true;

            if (module_version.Major > compare.Major)
            {
                is_older = false;
            }
            else if (module_version.Major == compare.Major)
            {
                if (module_version.Minor > compare.Minor)
                {
                    is_older = false;
                }
                else if (module_version.Minor == compare.Minor)
                {
                    if (module_version.Revision >= compare.Revision)
                    {
                        is_older = false;
                    }
                }
            }
            return is_older;
        }
        public static PeasantRevengeConfiguration CheckModules(PeasantRevengeConfiguration cfg_source)
        {
            string[] moduleNames = Utilities.GetModulesNames();
            //log("Checking modules for PeasantRevenge mod configuration... ");
            foreach (string modulesId in moduleNames)
            {
                //log($"{modulesId}");
                if (modulesId.Contains("Bannerlord.Diplomacy")) // Diplomacy mod patch
                {
                    bool need_patch = IsModuleVersionOlder(
                         TaleWorlds.ModuleManager.ModuleHelper.GetModuleInfo(modulesId).Version,
                         new ApplicationVersion(ApplicationVersionType.Release, 1, 2, 10, 0));

                    if (need_patch)
                    {
                        cfg_source.allowLordToKillMessenger = false;
                        cfg_source.allowPeasantToKillLord = false;
                    }
                }

                if (modulesId.Contains("Cutscenes_Extended"))
                {
                    cfg_source.allowPeasantToKillLord = false;
                }
            }

            return cfg_source;
        }
        public static void LoadConfiguration(CampaignGameStarter campaignGameStarter)
        {
            _cfg = new PeasantRevengeModCfg();
            int defaultVersion = new PeasantRevengeConfiguration().CfgVersion;

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
                    if (_cfg.values.CfgVersion < 19)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenLordPersuedeNotableNotToRevenge();
                        _cfg.values.ai.default_lordTraitChangeWhenLordPersuedeNotableToRevenge();
                        _cfg.values.ai.default_notableWillAcceptTheBribe();
                    }
                    if (_cfg.values.CfgVersion < 20)
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

                    if (_cfg.values.CfgVersion < 21)
                    {
                        _cfg.values.ai.default_lordPersuadeNotableExcludeTraitsAndRelationsWithNotable();
                    }

                    if (_cfg.values.CfgVersion < 22)
                    {
                        _cfg.values.ai.default_lordPersuadeNotableChooseExecuteTraitsAndRelationsWithSettlementOwner();
                        _cfg.values.ai.default_lordPersuadeNotableChooseExpelTraitsAndRelationsWithSettlementOwner();
                        _cfg.values.ai.default_lordPersuadeNotableChooseTeachTraitsAndRelationsWithSettlementOwner();
                    }

                    if (_cfg.values.CfgVersion < 24)
                    {
                        _cfg.values.ai.default_lordPersuadeNotableChooseBribeTraitsAndRelationsWithSettlementOwner();
                        _cfg.values.ai.default_lordPersuadeNotableWillAffordPartOfHisSavingsToPayForBribe();
                    }

                    if (_cfg.values.CfgVersion < 25)
                    {
                        _cfg.values.ai.default_lordTraitsOpposingPeasantsPower();
                        _cfg.values.ai.default_lordTraitsApprovePeasantsPower();
                    }

                    if (_cfg.values.CfgVersion < 26)
                    {
                        _cfg.values.ai.default_lastWordsIdPRev0149();
                        _cfg.values.ai.default_lastWordsIdPRev0150();
                        _cfg.values.ai.default_lastWordsIdPRev0151();
                        _cfg.values.ai.default_lastWordsIdPRev0152();
                        _cfg.values.ai.default_lastWordsIdPRev0153();
                        _cfg.values.ai.default_lastWordsIdPRev0154();
                        _cfg.values.ai.default_lastWordsIdPRev0155();
                        _cfg.values.ai.default_lastWordsIdPRev0156();
                    }

                    if (_cfg.values.CfgVersion < 27)
                    {
                        _cfg.values.ai.default_lastWordsIdPRev0168();
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
        }

        public static void ResetConfiguration()
        {
            _cfg = new PeasantRevengeModCfg();
            _cfg.values.ai = new PeasantRevengeConfiguration.AIfilters();
            _cfg.values.ai.Default();
        }

        public static void log(string text)
        {
            if (_cfg.values.disableMessages)
                return;
            TaleWorlds.Localization.TextObject textObject = new TaleWorlds.Localization.TextObject(text, null);
            InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Color.ConvertStringToColor(_cfg.values.logColorForClan)));
            if (!string.IsNullOrEmpty(_cfg.values.log_file_name))
            {
                File.AppendAllText(_cfg.values.log_file_name, $"{CampaignTime.Now}: {text}\r");
            }
        }

        public static bool can_remove_notable_from_village(Hero hero)
        {
            return (hero != null && hero.HomeSettlement != null && hero.HomeSettlement.Notables != null &&
                hero.HomeSettlement.Notables.Count > 1);
        }

        public static bool can_remove_notable_from_village_on_conversation()
        {
            return can_remove_notable_from_village(Hero.OneToOneConversationHero);
        }

        public static bool notable_can_do_revenge(Hero hero)
        {
            return !CfgParser.hero_trait_list_condition(hero, _cfg.values.peasantRevengerExcludeTrait, out string parseerror);
        }

        /// <summary>
        /// Checking hero (hero) traits and relations with another hero (target)
        /// </summary>
        /// <param name="hero">hero who has traits and relations with target hero</param>
        /// <param name="target">hero who relation is checked with hero</param>
        /// <param name="traits"></param>
        /// <returns></returns>
        public static bool CheckConditions(Hero hero, Hero target, List<PeasantRevengeConfiguration.RelationsPerTraits> traits)
        {
            if (traits.IsEmpty())
                return true;

            foreach (PeasantRevengeConfiguration.RelationsPerTraits rpt in traits)
            {
                if (CfgParser.hero_trait_list_condition(hero, rpt.relations, out string parseerror, target))
                {
                    if (CfgParser.hero_trait_list_condition(hero, rpt.traits, out parseerror, target))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public static bool CheckOnlyTraitsConditions(Hero hero, Hero target, List<PeasantRevengeConfiguration.RelationsPerTraits> traits)
        {
            if (traits.IsEmpty())
                return true;

            foreach (PeasantRevengeConfiguration.RelationsPerTraits rpt in traits)
            {
                if (CfgParser.hero_trait_list_condition(hero, rpt.traits, out string parseerror, target))
                {
                    return true;
                }
            }
            return false;
        }

        public static int GetHeroTraitValue(Hero hero, string tag)
        {
            int value = hero.GetTraitLevel(TraitObject.All.Where((x) => x.Name.ToString().Equals(tag)).FirstOrDefault());
            return value;
        }

        public static void SetHeroTraitValue(Hero hero, string tag, int value)
        {
            hero.SetTraitLevel(TraitObject.All.Where((x) => x.StringId.ToString() == tag).First(), value);
        }

        public static void TeachHeroTraits(Hero hero, string traits, bool direction, params Hero[] teacher)
        {
            if (string.IsNullOrEmpty(traits))
                return;

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
                    //log($"{hero.Name} learned new trait {a[0]} {(value > 0 ? "+" : "")}{value}");
                }
            }
        }

        public static string GetInfoStringForTraitsAndValues(List<PeasantRevengeConfiguration.TraitAndValue> traits_values)
        {
            string s = "";

            foreach (var traitAndValue in traits_values)
            {
                if (traitAndValue.value != 0)
                {
                    s += $"{traitAndValue.trait} {(traitAndValue.value > 0 ? "+" : "")}{traitAndValue.value} ";
                }
            }
            return s;
        }

        #region trait developement

        public static void OnLordUseTraitsAndValues(Hero hero, List<PeasantRevengeConfiguration.TraitAndValue> traits_and_values)
        {
            Tuple<TraitObject, int>[] traits = GetAffectedTraits(traits_and_values);
            if (hero != Hero.MainHero)
            {
                traits = ApplyProbabilityOfTraitChange(traits, (int)(_cfg.values.lordGainNewTraitProbability * 100.0));
            }
            if (traits.IsEmpty())
                return;
            OnChangeTraits(hero, traits);
        }

        public static void OnLordPersuedeNotableToRevenge(Hero hero)
        {
            OnLordUseTraitsAndValues(hero, _cfg.values.ai.lordTraitChangeWhenLordPersuedeNotableToRevenge);
        }
        public static void OnLordPersuedeNotableNotToRevenge(Hero hero)
        {
            OnLordUseTraitsAndValues(hero, _cfg.values.ai.lordTraitChangeWhenLordPersuedeNotableNotToRevenge);
        }

        public static void OnLordRemainsAbandoned(Hero hero)
        {
            OnLordUseTraitsAndValues(hero, _cfg.values.ai.lordTraitChangeWhenRemainsOfLordAreAbandoned);
        }

        public static void OnRansomRemainsOfferDeclined(Hero hero)
        {
            OnLordUseTraitsAndValues(hero, _cfg.values.ai.lordTraitChangeWhenRansomRemainsDeclined);
        }

        public static void OnRansomRemainsOfferAccepted(Hero hero)
        {
            OnLordUseTraitsAndValues(hero, _cfg.values.ai.lordTraitChangeWhenRansomRemainsAccepted);
        }

        public static void OnLordExecuteRevengerAfterOrBeforeQuest(Hero hero)
        {
            OnLordUseTraitsAndValues(hero, _cfg.values.ai.lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest);
        }

        public static Tuple<TraitObject, int>[] GetAffectedTraits(List<PeasantRevengeConfiguration.TraitAndValue> traitsAndValues)
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

        public static Tuple<TraitObject, int>[] ApplyProbabilityOfTraitChange(Tuple<TraitObject, int>[] traits, int chance)
        {

            if (traits == null || traits.Length == 0)
            {
                //log("No traits to apply probability.");
                return Array.Empty<Tuple<TraitObject, int>>();
            }


            int count = 0;
            bool[] indexes = new bool[traits.Length];

            for (int i = 0; i < traits.Length; i++)
            {
                int rchance = MBRandom.RandomInt(0, 100);
                //log($"rchance {i} : {rchance} vs chance {chance}");
                indexes[i] = rchance <= chance;
                if (indexes[i])
                {
                    count++;
                }
            }

            if (count == 0)
            {
                //log("No traits passed probability check.");
                return Array.Empty<Tuple<TraitObject, int>>();
            }


            Tuple<TraitObject, int>[] appliedTraits = new Tuple<TraitObject, int>[count];
            int index = 0;
            for (int i = 0; i < traits.Length; i++)
            {
                if (indexes[i])
                {
                    appliedTraits[index] = traits[i];
                    index++;
                }
            }

            return appliedTraits;
        }

        public static void OnChangeTraits(Hero targetHero, Tuple<TraitObject, int>[] effectedTraits)
        {
            foreach (Tuple<TraitObject, int> tuple in effectedTraits)
            {
                ApplyTraitXP(tuple.Item1, tuple.Item2, ActionNotes.DefaultNote, targetHero);
            }
        }
        public static void ApplyTraitXP(TraitObject trait, int xpValue, ActionNotes context, Hero hero)
        {
            if (hero.IsHumanPlayerCharacter)
            {
                int traitLevel = hero.GetTraitLevel(trait);
                int xp = Campaign.Current.PlayerTraitDeveloper.GetPropertyValue(trait);
                xp += xpValue;
                Campaign.Current.PlayerTraitDeveloper.SetPropertyValue(trait, xp);
                if (traitLevel != hero.GetTraitLevel(trait))
                {
                    CampaignEventDispatcher.Instance.OnPlayerTraitChanged(trait, traitLevel);
                }
                //log($"{hero.Name} {trait.Name} new xp: {xp}.");
            }
            else
            {
                if (xpValue != 0)
                {
                    int oldTraitLevel = hero.GetTraitLevel(trait);
                    int traitLevel = oldTraitLevel + xpValue;
                    traitLevel = MBMath.ClampInt(traitLevel, trait.MinValue, trait.MaxValue);
                    //log($"{trait.Name} min {trait.MinValue},max {trait.MaxValue}.");
                    if (traitLevel != oldTraitLevel)
                    {
                        SetHeroTraitValue(hero, trait.Name.ToString(), traitLevel);
                        //log($"{hero.Name} new {trait.Name} is {traitLevel} (was {oldTraitLevel}).");
                    }
                }
            }
        }
        #endregion

        /*
         See usage in GetPersuasionTask()
         */
        public static List<PeasantRevengeConfiguration.TraitAndValue> GetTraitsAndValuesByTaskAndOption(int task_index, int option)
        {
            switch (task_index)
            {
                case 0:
                    switch (option)
                    {
                        case 0:
                            return _cfg.values.ai.PersuadeNotableToRevengeTraitsForOption0;
                        case 1:
                            return _cfg.values.ai.PersuadeNotableToRevengeTraitsForOption1;
                        case 2:
                            return _cfg.values.ai.PersuadeNotableToRevengeTraitsForOption2;
                        default:
                            return null;
                    }
                case 1:
                    switch (option)
                    {
                        case 0:
                            return _cfg.values.ai.PersuadeNotableNotToRevengeTraitsForOption0;
                        case 1:
                            return _cfg.values.ai.PersuadeNotableNotToRevengeTraitsForOption1;
                        case 2:
                            return _cfg.values.ai.PersuadeNotableNotToRevengeTraitsForOption2;
                        default:
                            return null;
                    }
                case 2:
                    switch (option)
                    {
                        case 0:
                            return _cfg.values.ai.AccuseNotableTraitsForOption0;
                        case 1:
                            return _cfg.values.ai.AccuseNotableTraitsForOption1;
                        case 2:
                            return _cfg.values.ai.AccuseNotableTraitsForOption2;
                        default:
                            return null;
                    }
                default:
                    return null;
            }
        }

        public static int GetTaskIndexByPersuadeStatus(event_status persuade)
        {
            switch (persuade)
            {
                case event_status.bribe:
                    return 3; /*TODO: Bribing persuation task game - hero could say: Lets talk about "gold icon"... 0: Everything has price; 1: Nobody will know...; 2: Your farm needs some repairs; 3: Just a gift...; ...  while bribing*/
                case event_status.teach_to_revenge:
                    return 0;
                case event_status.teach_to_not_revenge:
                    return 1;
                case event_status.accusation:
                    return 2;
                default:
                    return 0;
            }
        }

        /*Hero will choose option what is the most similar to hero traits */
        public static int GetOptionIndexByHeroTraits(Hero hero, int task_index)
        {
            int option_count = 3;
            int option = 0;

            int[] option_cor = { 0, 0, 0 };

            int max_cor_option_val = 0;
            int max_cor_option_ind = -1;

            for (; option < option_count; option++)
            {
                Tuple<TraitObject, int>[] affected_traits = GetAffectedTraits(GetTraitsAndValuesByTaskAndOption(task_index, option));

                option_cor[option] = affected_traits[option].Item2 * GetHeroTraitValue(hero, affected_traits[option].Item1.Name.ToString());

            }

            for (option = 0; option < option_count; option++)
            {
                if (max_cor_option_val < option_cor[option])
                {
                    max_cor_option_val = option_cor[option];
                    max_cor_option_ind = option;
                }
            }

            if (max_cor_option_ind == -1)
            {
                Random random = new Random(0);
                max_cor_option_ind = random.Next(0, 2);
            }

            /*TODO use random choise when more options have same value*/

            return max_cor_option_ind;
        }
        public static int get_notable_bribe_amount(Hero hero)
        {
            int bribe_percents = _cfg.values.goldPercentOfPeasantTotallGoldToTeachPeasantToBeLoyal;
            int bribe = hero.Gold * bribe_percents / 100;
            return bribe;
        }

        public static bool CanAffordToSpendMoney(Hero hero, int goldNeeded, List<PeasantRevengeConfiguration.MoneyPerTraits> traits)
        {
            if (hero.Gold == 0 || hero.Gold < goldNeeded)
                return false;

            int percent = 100 * goldNeeded / hero.Gold;

            foreach (PeasantRevengeConfiguration.MoneyPerTraits mpt in traits)
            {
                if (mpt.percent >= percent)
                {
                    if (CfgParser.hero_trait_list_condition(hero, mpt.traits, out string parseerror))
                    {
                        return true;
                    }
                }
            }
            return true;
        }

        public static void OnHeroChopNotableHeadConsequence(Hero executioner_hero, Hero victim)
        {
            bool chop_purpose = notable_can_do_revenge(victim); // true if notable can do the revenge, but hero want to prohibit            

            foreach (Hero notable in victim.HomeSettlement.Notables)
            {
                if (notable != victim)
                {
                    int nobles_relations = notable.GetRelation(victim);// smaller the relation - bigger chance to get positive result towards player
                    int hero_noble_relation = notable.GetRelation(executioner_hero); // bigger the relation - bigger chance to get positive result towards player 
                    int relation_change = (hero_noble_relation > nobles_relations) ? _cfg.values.relationChangeWhenLordTeachPeasant : -_cfg.values.relationChangeWhenLordTeachPeasant;
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(executioner_hero, notable, relation_change, true);
                    if (_cfg.values.enableOtherNobleTraitsChangeAfterNobleExecution)
                    {
                        bool direction = MBRandom.RandomInt(-100, 100) < hero_noble_relation; // bigger relation means bigger chance direction is similar to chop purpose
                        TeachHeroTraits(notable, _cfg.values.peasantRevengerExcludeTrait, chop_purpose ? direction : !direction);
                    }
                }
            }
        }
    }
}
