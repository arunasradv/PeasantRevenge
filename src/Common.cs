using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PeasantRevenge
{
    public static class Common
    {
        public static PeasantRevengeModCfg _cfg = new PeasantRevengeModCfg();

        public static void ResetConfiguration()
        {
            _cfg=new PeasantRevengeModCfg();
            _cfg.values.ai=new PeasantRevengeConfiguration.AIfilters();
            _cfg.values.ai.Default();
        }

        public static void log(string text)
        {
            if(!string.IsNullOrEmpty(_cfg.values.log_file_name))
            {
                TaleWorlds.Localization.TextObject textObject = new TaleWorlds.Localization.TextObject(text,null);
                InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(),Color.ConvertStringToColor(_cfg.values.logColorForClan)));
                
                File.AppendAllText(_cfg.values.log_file_name,$"{CampaignTime.Now}: {text}\r");
            }
        }

        public static bool notable_can_do_revenge(Hero hero)
        {
            return !hero_trait_list_condition(hero,_cfg.values.peasantRevengerExcludeTrait);
        }

        /// <summary>
        /// Checking hero (hero) traits and relations with another hero (target)
        /// </summary>
        /// <param name="hero">hero who has traits and relations with target hero</param>
        /// <param name="target">hero who relation is checked with hero</param>
        /// <param name="traits"></param>
        /// <returns></returns>
        public static bool CheckConditions(Hero hero,Hero target,List<PeasantRevengeConfiguration.RelationsPerTraits> traits)
        {
            if(traits.IsEmpty())
                return true;

            foreach(PeasantRevengeConfiguration.RelationsPerTraits rpt in traits)
            {
                if(hero_trait_list_condition(hero,rpt.relations,target))
                {
                    if(hero_trait_list_condition(hero,rpt.traits,target))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public static bool CheckOnlyTraitsConditions(Hero hero,Hero target,List<PeasantRevengeConfiguration.RelationsPerTraits> traits)
        {
            if(traits.IsEmpty())
                return true;

            foreach(PeasantRevengeConfiguration.RelationsPerTraits rpt in traits)
            {
                if(hero_trait_list_condition(hero,rpt.traits,target))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool hero_relation_on_condition(Hero hero,Hero target,string operation,string weight)
        {
            if(hero==null||target==null)
                return false;
            return get_result(hero.GetRelation(target),operation,weight);         
        }

        public static bool hero_trait_on_condition(Hero hero,string tag,string operation,string weight)
        {
            if(hero==null)
                return false;
            return get_result(GetHeroTraitValue(hero,tag), operation, weight);
        }

        static bool get_result(int value,string operation,string weight)
        {
            bool result = operation=="==" ? value==int.Parse(weight) :
                         operation==">=" ? value>=int.Parse(weight) :
                         operation=="<=" ? value<=int.Parse(weight) :
                         operation==">" ? value>int.Parse(weight) :
                         operation=="<" ? value<int.Parse(weight) :
                         operation=="!=" ? value!=int.Parse(weight) : false;
            return result;
        }


        public static bool hero_trait_list_condition(Hero hero,string conditions,params Hero [] target)
        {
            if(string.IsNullOrEmpty(conditions))
                return true;

            string [] equation;

            conditions.Replace(";","&"); // compatibility

            equation=conditions.Split('|');

            bool result = false;

            foreach(string equationItem in equation)
            {
                bool ANDresult = false;
                if(equationItem.Contains("&"))
                {
                    ANDresult=true;
                    string [] equationAND = equationItem.Split('&');
                    for(int i = 0;i<equationAND.Length;i++)
                    {
                        string [] a = equationAND [i].Split(' ');
                        if(a.Length==3)
                        {
                            if(a [0]=="Relations")
                            {
                                for(int k = 0;k<target.Length;k++)
                                {
                                    ANDresult=ANDresult&&hero_relation_on_condition(hero,target [k],a [1],a [2]);
                                }
                            }
                            else
                            {
                                ANDresult=ANDresult&&hero_trait_on_condition(hero,a [0],a [1],a [2]);
                            }
                        }
                        else
                        {
                            //log("Error in equation: "+equationAND.ToString()+". Now will be using default cfg. Please fix or Delete cfg file.");
                            ResetConfiguration();
                            break;
                        }
                    }
                }
                else
                {
                    string [] a = equationItem.Split(' ');
                    if(a.Length==3)
                    {
                        if(a [0]=="Relations")
                        {
                            for(int k = 0;k<target.Length;k++)
                            {
                                ANDresult=hero_relation_on_condition(hero,target [k],a [1],a [2]);
                            }
                        }
                        else
                        {
                            ANDresult=hero_trait_on_condition(hero,a [0],a [1],a [2]);
                        }
                    }
                    else
                    {
                        //log("Error in equation: "+equationItem.ToString()+". Now will be using default cfg. Please fix or Delete cfg file.");
                        ResetConfiguration();
                        break;
                    }
                }

                result=result||ANDresult;
            }

            return result;
        }

        public  static int GetHeroTraitValue(Hero hero,string tag)
        {
            CharacterTraits ht = hero.GetHeroTraits();
            PropertyInfo [] props = ht.GetType().GetProperties();
            var prop = props.Where((x) => x.Name==tag).FirstOrDefault();
            if(prop==null)
            {
                return 0;
            }

            int value = (int)prop.GetValue((object)ht);

            return value;
        }

        public static void SetHeroTraitValue(Hero hero,string tag,int value)
        {
            hero.SetTraitLevel(TraitObject.All.Where((x) => x.StringId.ToString()==tag).First(),value);
        }

        public static void TeachHeroTraits(Hero hero,string traits,bool direction,params Hero [] teacher)
        {
            if(string.IsNullOrEmpty(traits))
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

                    if(!teacher.IsEmpty())
                    {
                        int target = GetHeroTraitValue(teacher.First(),a [0]);

                        if(a [1].Contains(">"))
                        {
                            value=value>target ? direction ? value : target : direction ? target : value;
                        }
                        else if(a [1].Contains("<"))
                        {
                            value=value<target ? direction ? value : target : direction ? target : value;
                        }
                        else if(a [1].Contains("=="))
                        {
                            value=value==target ? value : value>target ? direction ? value : target : direction ? target : value;
                        }
                    }
                    else
                    {
                        int target = int.Parse(a [2]);

                        if(a [1].Contains(">"))
                        {
                            value=direction ? target+1 : target-1;
                        }
                        else if(a [1].Contains("<"))
                        {
                            value=direction ? target-1 : target+1;
                        }
                        else if(a [1].Contains("=="))
                        {
                            value=target;
                        }
                    }

                    SetHeroTraitValue(hero,$"{a [0]}",value);
                }
            }
        }
    }
}
