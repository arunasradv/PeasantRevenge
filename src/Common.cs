using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using static PeasantRevenge.PeasantRevengeConfiguration;

namespace PeasantRevenge
{
    public static class Common
    {
        public static PeasantRevengeModCfg _cfg;

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

        private static bool IsModuleVersionOlder (ApplicationVersion module_version ,ApplicationVersion compare)
        {
            bool is_older = true;

            if(module_version.Major > compare.Major)
            {
                is_older = false;
            }
            else if(module_version.Major == compare.Major)
            {
                if(module_version.Minor > compare.Minor)
                {
                    is_older = false;
                }
                else if(module_version.Minor == compare.Minor)
                {
                    if(module_version.Revision >= compare.Revision)
                    {
                        is_older = false;
                    }
                }
            }
            return is_older;
        }
        public static PeasantRevengeConfiguration CheckModules (PeasantRevengeConfiguration cfg_source)
        {
            string[] moduleNames = Utilities.GetModulesNames();

            foreach(string modulesId in moduleNames)
            {
                if(modulesId.Contains ("Bannerlord.Diplomacy")) // Diplomacy mod patch
                {
                    bool need_patch = IsModuleVersionOlder(
                         TaleWorlds.ModuleManager.ModuleHelper.GetModuleInfo(modulesId).Version,
                         new ApplicationVersion(ApplicationVersionType.Release,1,2,10,0));

                    if(need_patch)
                    {
                        cfg_source.allowLordToKillMessenger = false;
                        cfg_source.allowPeasantToKillLord = false;
                    }
                    break; // because there is no more module patches it should end the configuration
                }
            }

            return cfg_source;
        }
        public static void LoadConfiguration (CampaignGameStarter campaignGameStarter)
        {
            _cfg = new PeasantRevengeModCfg ( );
            int defaultVersion = new PeasantRevengeConfiguration().CfgVersion;

            if(File.Exists (_cfg.values.file_name))
            {
                _cfg.Load (_cfg.values.file_name ,typeof (PeasantRevengeConfiguration));

                if(_cfg.values.ai == null)
                {
                    _cfg.values.ai = new PeasantRevengeConfiguration.AIfilters ( );
                    _cfg.values.ai.Default ( );
                }
                else
                {
                    // configuration patch for new added configuration variables

                    if(_cfg.values.CfgVersion < 14)
                    {
                        _cfg.values.ai.default_criminalWillBlameOtherLordForTheCrime ( );
                        _cfg.values.ai.default_lordWillKillBothAccusedHeroAndCriminalLord ( );
                    }

                    if(_cfg.values.CfgVersion < 15)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenRansomRemainsDeclined ( );
                        _cfg.values.ai.default_lordTraitChangeWhenRansomRemainsAccepted ( );
                        _cfg.values.ai.default_lordTraitChangeWhenRemainsOfLordAreAbandoned ( );
                        _cfg.values.ai.default_lordWillDeclineRansomTheVictimRemains ( );
                        _cfg.values.ai.default_lordWillAbandonTheVictimRemains ( );
                    }

                    if(_cfg.values.CfgVersion < 16)
                    {
                        _cfg.values.ai.default_lordWillNotKillBothAccusedHeroAndCriminalLordDueConflict ( );
                    }

                    if(_cfg.values.CfgVersion < 17)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest ( );
                    }
                    if(_cfg.values.CfgVersion < 19)
                    {
                        _cfg.values.ai.default_lordTraitChangeWhenLordPersuedeNotableNotToRevenge ( );
                        _cfg.values.ai.default_lordTraitChangeWhenLordPersuedeNotableToRevenge ( );
                        _cfg.values.ai.default_notableWillAcceptTheBribe ( );
                    }
                    if(_cfg.values.CfgVersion < 20)
                    {
                        _cfg.values.ai.default_PersuadeNotableToRevengeTraitsForOption0 ( );
                        _cfg.values.ai.default_PersuadeNotableToRevengeTraitsForOption1 ( );
                        _cfg.values.ai.default_PersuadeNotableToRevengeTraitsForOption2 ( );
                        _cfg.values.ai.default_PersuadeNotableNotToRevengeTraitsForOption0 ( );
                        _cfg.values.ai.default_PersuadeNotableNotToRevengeTraitsForOption1 ( );
                        _cfg.values.ai.default_PersuadeNotableNotToRevengeTraitsForOption2 ( );
                        _cfg.values.ai.default_AccuseNotableTraitsForOption0 ( );
                        _cfg.values.ai.default_AccuseNotableTraitsForOption1 ( );
                        _cfg.values.ai.default_AccuseNotableTraitsForOption2 ( );
                    }

                    if(_cfg.values.CfgVersion < 21)
                    {
                        _cfg.values.ai.default_lordPersuadeNotableExcludeTraitsAndRelationsWithNotable ( );
                    }

                    if(_cfg.values.CfgVersion < 22)
                    {
                        _cfg.values.ai.default_lordPersuadeNotableChooseExecuteTraitsAndRelationsWithSettlementOwner ( );
                        _cfg.values.ai.default_lordPersuadeNotableChooseExpelTraitsAndRelationsWithSettlementOwner ( );
                        _cfg.values.ai.default_lordPersuadeNotableChooseTeachTraitsAndRelationsWithSettlementOwner ( );
                    }

                    if(_cfg.values.CfgVersion < 24)
                    {
                        _cfg.values.ai.default_lordPersuadeNotableChooseBribeTraitsAndRelationsWithSettlementOwner ( );
                        _cfg.values.ai.default_lordPersuadeNotableWillAffordPartOfHisSavingsToPayForBribe ( );
                    }
                }
            }
            else
            {
                if(_cfg.values.ai == null)
                {
                    _cfg.values.ai = new PeasantRevengeConfiguration.AIfilters ( );
                    _cfg.values.ai.Default ( );
                }
            }

            _cfg.values = CheckModules (_cfg.values); // leave loaded cfg or change cfg only if needed !

            if(defaultVersion > _cfg.values.CfgVersion || !File.Exists (_cfg.values.file_name))
            {
                #region configuration patch

                if(_cfg.values.CfgVersion == 14)
                {
                    _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge =
                        _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge == -2 ? -1 : _cfg.values.relationChangeWhenLordRefusedToSupportPeasantRevenge; //reduced, because lords may lose recruitement village too fast
                }

                _cfg.values.CfgVersion = defaultVersion;
                #endregion

                bool can_save = false;

                try
                {
                    if(Directory.GetDirectories (_cfg.values.file_name) != null)
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
                    if(!can_save)
                    {
                        _cfg.values.file_name = PeasantRevengeConfiguration.default_file_name ( );
                    }
                }

                _cfg.Save (_cfg.values.file_name ,_cfg.values);
            }
        }

        public static void ResetConfiguration ()
        {
            _cfg = new PeasantRevengeModCfg ( );
            _cfg.values.ai = new PeasantRevengeConfiguration.AIfilters ( );
            _cfg.values.ai.Default ( );
        }

        public static void log (string text)
        {
            TaleWorlds.Localization.TextObject textObject = new TaleWorlds.Localization.TextObject(text,null);
            InformationManager.DisplayMessage (new InformationMessage (textObject.ToString ( ) ,Color.ConvertStringToColor (_cfg.values.logColorForClan)));
            if(!string.IsNullOrEmpty (_cfg.values.log_file_name))
            {
                File.AppendAllText (_cfg.values.log_file_name ,$"{CampaignTime.Now}: {text}\r");
            }
        }

        public static bool can_remove_notable_from_village (Hero hero)
        {
            return (hero != null && hero.HomeSettlement != null && hero.HomeSettlement.Notables != null &&
                hero.HomeSettlement.Notables.Count > 1);
        }

        public static bool can_remove_notable_from_village_on_conversation ()
        {
            return can_remove_notable_from_village (Hero.OneToOneConversationHero);
        }

        public static bool notable_can_do_revenge (Hero hero)
        {
            return !hero_trait_list_condition (hero ,_cfg.values.peasantRevengerExcludeTrait);
        }

        /// <summary>
        /// Checking hero (hero) traits and relations with another hero (target)
        /// </summary>
        /// <param name="hero">hero who has traits and relations with target hero</param>
        /// <param name="target">hero who relation is checked with hero</param>
        /// <param name="traits"></param>
        /// <returns></returns>
        public static bool CheckConditions (Hero hero ,Hero target ,List<PeasantRevengeConfiguration.RelationsPerTraits> traits)
        {
            if(traits.IsEmpty ( ))
                return true;

            foreach(PeasantRevengeConfiguration.RelationsPerTraits rpt in traits)
            {
                if(hero_trait_list_condition (hero ,rpt.relations ,target))
                {
                    if(hero_trait_list_condition (hero ,rpt.traits ,target))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public static bool CheckOnlyTraitsConditions (Hero hero ,Hero target ,List<PeasantRevengeConfiguration.RelationsPerTraits> traits)
        {
            if(traits.IsEmpty ( ))
                return true;

            foreach(PeasantRevengeConfiguration.RelationsPerTraits rpt in traits)
            {
                if(hero_trait_list_condition (hero ,rpt.traits ,target))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool hero_relation_on_condition (Hero hero ,Hero target ,string operation ,string weight)
        {
            if(hero == null || target == null)
                return false;
            return get_result (hero.GetRelation (target) ,operation ,weight);
        }

        public static bool hero_trait_on_condition (Hero hero ,string tag ,string operation ,string weight)
        {
            if(hero == null)
                return false;
            return get_result (GetHeroTraitValue (hero ,tag) ,operation ,weight);
        }

        static bool get_result (int value ,string operation ,string weight)
        {
            bool result = operation=="==" ? value==int.Parse(weight) :
                         operation==">=" ? value>=int.Parse(weight) :
                         operation=="<=" ? value<=int.Parse(weight) :
                         operation==">" ? value>int.Parse(weight) :
                         operation=="<" ? value<int.Parse(weight) :
                         operation=="!=" ? value!=int.Parse(weight) : false;
            return result;
        }


        public static bool hero_trait_list_condition (Hero hero ,string conditions ,params Hero [] target)
        {
            if(string.IsNullOrEmpty (conditions))
                return true;

            string [] equation;

            conditions.Replace (";" ,"&"); // compatibility

            equation = conditions.Split ('|');

            bool result = false;

            foreach(string equationItem in equation)
            {
                bool ANDresult = false;
                if(equationItem.Contains ("&"))
                {
                    ANDresult = true;
                    string [] equationAND = equationItem.Split('&');
                    for(int i = 0;i < equationAND.Length;i++)
                    {
                        string [] a = equationAND [i].Split(' ');
                        if(a.Length == 3)
                        {
                            if(a [0] == "Relations")
                            {
                                for(int k = 0;k < target.Length;k++)
                                {
                                    ANDresult = ANDresult && hero_relation_on_condition (hero ,target [k] ,a [1] ,a [2]);
                                }
                            }
                            else
                            {
                                ANDresult = ANDresult && hero_trait_on_condition (hero ,a [0] ,a [1] ,a [2]);
                            }
                        }
                        else
                        {
                            //log("Error in equation: "+equationAND.ToString()+". Now will be using default cfg. Please fix or Delete cfg file.");
                            ResetConfiguration ( );
                            break;
                        }
                    }
                }
                else
                {
                    string [] a = equationItem.Split(' ');
                    if(a.Length == 3)
                    {
                        if(a [0] == "Relations")
                        {
                            for(int k = 0;k < target.Length;k++)
                            {
                                ANDresult = hero_relation_on_condition (hero ,target [k] ,a [1] ,a [2]);
                            }
                        }
                        else
                        {
                            ANDresult = hero_trait_on_condition (hero ,a [0] ,a [1] ,a [2]);
                        }
                    }
                    else
                    {
                        //log("Error in equation: "+equationItem.ToString()+". Now will be using default cfg. Please fix or Delete cfg file.");
                        ResetConfiguration ( );
                        break;
                    }
                }

                result = result || ANDresult;
            }

            return result;
        }

        public static int GetHeroTraitValue (Hero hero ,string tag)
        {
            CharacterTraits ht = hero.GetHeroTraits();
            PropertyInfo [] props = ht.GetType().GetProperties();
            var prop = props.Where((x) => x.Name==tag).FirstOrDefault();
            if(prop == null)
            {
                return 0;
            }

            int value = (int)prop.GetValue((object)ht);

            return value;
        }

        public static void SetHeroTraitValue (Hero hero ,string tag ,int value)
        {
            hero.SetTraitLevel (TraitObject.All.Where ((x) => x.StringId.ToString ( ) == tag).First ( ) ,value);
        }

        public static void TeachHeroTraits (Hero hero ,string traits ,bool direction ,params Hero [] teacher)
        {
            if(string.IsNullOrEmpty (traits))
                return;

            List<string> traits_con_pool = traits.Split('|').ToList();
            string [] traits_con = traits_con_pool.ToArray();

            foreach(string trait_or in traits_con)
            {
                string [] trait_or_con = trait_or.Split('&');

                foreach(string trait in trait_or_con)
                {
                    string [] a = trait.Split(' ');
                    int value = GetHeroTraitValue(hero,a [0]);

                    if(!teacher.IsEmpty ( ))
                    {
                        int target = GetHeroTraitValue(teacher.First(),a [0]);

                        if(a [1].Contains (">"))
                        {
                            value = value > target ? direction ? value : target : direction ? target : value;
                        }
                        else if(a [1].Contains ("<"))
                        {
                            value = value < target ? direction ? value : target : direction ? target : value;
                        }
                        else if(a [1].Contains ("=="))
                        {
                            value = value == target ? value : value > target ? direction ? value : target : direction ? target : value;
                        }
                    }
                    else
                    {
                        int target = int.Parse(a [2]);

                        if(a [1].Contains (">"))
                        {
                            value = direction ? target + 1 : target - 1;
                        }
                        else if(a [1].Contains ("<"))
                        {
                            value = direction ? target - 1 : target + 1;
                        }
                        else if(a [1].Contains ("=="))
                        {
                            value = target;
                        }
                    }

                    SetHeroTraitValue (hero ,$"{a [0]}" ,value);
                    log ($"{hero.Name} learned new trait {a [0]} {(value > 0 ? "+" : "")}{value}");
                }
            }
        }

        public static string GetInfoStringForTraitsAndValues (List<PeasantRevengeConfiguration.TraitAndValue> traits_values)
        {
            string s = "";

            foreach(var traitAndValue in traits_values)
            {
                if(traitAndValue.value != 0)
                {
                    s += $"{traitAndValue.trait} {(traitAndValue.value > 0 ? "+" : "")}{traitAndValue.value} ";
                }
            }
            return s;
        }

        #region trait developement

        public static void OnLordPersuedeNotableUseTraitsAndValues (Hero hero ,List<PeasantRevengeConfiguration.TraitAndValue> traits_and_values)
        {
            log ($"{hero.Name} {GetInfoStringForTraitsAndValues (traits_and_values)}");
            OnChangeTraits (hero ,GetAffectedTraits (traits_and_values));
        }

        public static void OnLordPersuedeNotableToRevenge (Hero hero)
        {
            log ($"{hero.Name} {GetInfoStringForTraitsAndValues (_cfg.values.ai.lordTraitChangeWhenLordPersuedeNotableToRevenge)}");
            OnChangeTraits (hero ,GetAffectedTraits (_cfg.values.ai.lordTraitChangeWhenLordPersuedeNotableToRevenge));
        }
        public static void OnLordPersuedeNotableNotToRevenge (Hero hero)
        {
            log ($"{hero.Name} {GetInfoStringForTraitsAndValues (_cfg.values.ai.lordTraitChangeWhenLordPersuedeNotableNotToRevenge)}");
            OnChangeTraits (hero ,GetAffectedTraits (_cfg.values.ai.lordTraitChangeWhenLordPersuedeNotableNotToRevenge));
        }

        public static void OnLordRemainsAbandoned (Hero hero)
        {
            log ($"{hero.Name} {GetInfoStringForTraitsAndValues (_cfg.values.ai.lordTraitChangeWhenRemainsOfLordAreAbandoned)}");
            OnChangeTraits (hero ,GetAffectedTraits (_cfg.values.ai.lordTraitChangeWhenRemainsOfLordAreAbandoned));
        }

        public static void OnRansomRemainsOfferDeclined (Hero hero)
        {
            log ($"{hero.Name} {GetInfoStringForTraitsAndValues (_cfg.values.ai.lordTraitChangeWhenRansomRemainsDeclined)}");
            OnChangeTraits (hero ,GetAffectedTraits (_cfg.values.ai.lordTraitChangeWhenRansomRemainsDeclined));
        }

        public static void OnRansomRemainsOfferAccepted (Hero hero)
        {
            log ($"{hero.Name} {GetInfoStringForTraitsAndValues (_cfg.values.ai.lordTraitChangeWhenRansomRemainsAccepted)}");
            OnChangeTraits (hero ,GetAffectedTraits (_cfg.values.ai.lordTraitChangeWhenRansomRemainsAccepted));
        }

        public static void OnLordExecuteRevengerAfterOrBeforeQuest (Hero hero)
        {
            log ($"{hero.Name} {GetInfoStringForTraitsAndValues (_cfg.values.ai.lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest)}");
            OnChangeTraits (hero ,GetAffectedTraits (_cfg.values.ai.lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest));
        }

        public static Tuple<TraitObject ,int> [] GetAffectedTraits (List<PeasantRevengeConfiguration.TraitAndValue> traitsAndValues)
        {
            Tuple<TraitObject, int>[] affectedTraits = new Tuple<TraitObject, int>[traitsAndValues.Count];
            for(int i = 0;i < affectedTraits.Length;i++)
            {
                affectedTraits [i] = Tuple.Create (
                    TraitObject.All.Where ((x) => x.StringId.ToString ( ) ==
                    traitsAndValues [i].trait).First ( ) ,
                    traitsAndValues [i].value);
            }

            return affectedTraits;
        }

        public static void OnChangeTraits (Hero targetHero ,Tuple<TraitObject ,int> [] effectedTraits)
        {
            foreach(Tuple<TraitObject ,int> tuple in effectedTraits)
            {
                ApplyTraitXP (tuple.Item1 ,tuple.Item2 ,ActionNotes.DefaultNote ,targetHero);
            }
        }
        public static void ApplyTraitXP (TraitObject trait ,int xpValue ,ActionNotes context ,Hero hero)
        {
            if(hero == Hero.MainHero)
            {
                int traitLevel = hero.GetTraitLevel(trait);
                Campaign.Current.PlayerTraitDeveloper.AddTraitXp (trait ,xpValue);
                if(traitLevel != hero.GetTraitLevel (trait))
                {
                    CampaignEventDispatcher.Instance.OnPlayerTraitChanged (trait ,traitLevel);
                }
            }
            else
            {
                if(xpValue != 0)
                {
                    int oldTraitLevel = hero.GetTraitLevel(trait);
                    int traitLevel = oldTraitLevel + xpValue;
                    SetHeroTraitValue (hero ,trait.Name.ToString ( ) ,traitLevel);
                    log ($"{hero.Name} new {trait.Name} is {traitLevel} (was {oldTraitLevel}).");
                }
            }
        }
        #endregion

        /*
         See usage in GetPersuasionTask()
         */
        public static List<PeasantRevengeConfiguration.TraitAndValue> GetTraitsAndValuesByTaskAndOption (int task_index ,int option)
        {
            switch(task_index)
            {
                case 0:
                    switch(option)
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
                    switch(option)
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
                    switch(option)
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

        public static int GetTaskIndexByPersuadeStatus (persuade_type persuade)
        {
            switch(persuade)
            {
                case persuade_type.bribe:
                    return 3; /*TODO: Bribing persuation task game - hero could say: Lets talk about "gold icon"... 0: Everything has price; 1: Nobody will know...; 2: Your farm needs some repairs; 3: Just a gift...; ...  while bribing*/
                case persuade_type.teach_to_revenge:
                    return 0;
                case persuade_type.teach_to_not_revenge:
                    return 1;
                case persuade_type.accusation:
                    return 2;
                default:
                    return 0;
            }
        }

        /*Hero will choose option what is the most similar to hero traits */
        public static int GetOptionIndexByHeroTraits (Hero hero ,int task_index)
        {
            int option_count = 3;
            int option = 0;

            int[] option_cor = { 0,0,0};

            int max_cor_option_val  = 0;
            int max_cor_option_ind  = -1;

            for(;option < option_count;option++)
            {
                Tuple<TraitObject ,int> [] affected_traits = GetAffectedTraits(GetTraitsAndValuesByTaskAndOption ( task_index, option));

                option_cor [option] = affected_traits [option].Item2 * GetHeroTraitValue (hero ,affected_traits [option].Item1.Name.ToString ( ));

            }

            for(option = 0;option < option_count;option++)
            {
                if(max_cor_option_val < option_cor [option])
                {
                    max_cor_option_val = option_cor [option];
                    max_cor_option_ind = option;
                }
            }

            if(max_cor_option_ind == -1)
            {
                Random random = new Random(0);
                max_cor_option_ind = random.Next (0 ,2);
            }

            /*TODO use random choise when more options have same value*/

            return max_cor_option_ind;
        }
        public static int get_notable_bribe_amount (Hero hero)
        {
            int bribe_percents = _cfg.values.goldPercentOfPeasantTotallGoldToTeachPeasantToBeLoyal;
            int bribe = hero.Gold * bribe_percents / 100;
            return bribe;
        }

        public static bool CanAffordToSpendMoney (Hero hero ,int goldNeeded ,List<PeasantRevengeConfiguration.MoneyPerTraits> traits)
        {
            if(hero.Gold == 0 || hero.Gold < goldNeeded)
                return false;

            int percent = 100 * goldNeeded / hero.Gold;

            foreach(PeasantRevengeConfiguration.MoneyPerTraits mpt in traits)
            {
                if(mpt.percent >= percent)
                {
                    if(hero_trait_list_condition (hero ,mpt.traits))
                    {
                        return true;
                    }
                }
            }
            return true;
        }

        public static void OnHeroChopNotableHeadConsequence (Hero executioner_hero ,Hero victim)
        {
            bool chop_purpose = notable_can_do_revenge(victim); // true if notable can do the revenge, but hero want to prohibit            

            foreach(Hero hero in victim.HomeSettlement.Notables)
            {
                if(hero != victim)
                {
                    int nobles_relations = hero.GetRelation(victim);// smaller the relation - bigger chance to get positive result towards player
                    int hero_noble_relation = hero.GetRelation(executioner_hero); // bigger the relation - bigger chance to get positive result towards player 
                    int relation_change = (hero_noble_relation > nobles_relations) ? _cfg.values.relationChangeWhenLordTeachPeasant : -_cfg.values.relationChangeWhenLordTeachPeasant;
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes (executioner_hero ,hero ,relation_change ,true);
                    if(_cfg.values.enableOtherNobleTraitsChangeAfterNobleExecution)
                    {
                        bool direction = MBRandom.RandomInt(-100, 100) < hero_noble_relation; // bigger relation means bigger chance direction is similar to chop purpose
                        TeachHeroTraits (hero ,_cfg.values.peasantRevengerExcludeTrait ,chop_purpose ? direction : !direction);
                    }
                }
            }
        }
    }
}
