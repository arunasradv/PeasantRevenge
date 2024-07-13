using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace PeasantRevenge
{
    public class CfgParser
    {
        private static bool hero_relation_on_condition(Hero hero,Hero target,string operation,string weight)
        {
            if(hero==null||target==null)
                return false;

            int value = hero.GetRelation(target);

            bool result = operation=="==" ? value==int.Parse(weight) :
                          operation==">=" ? value>=int.Parse(weight) :
                          operation=="<=" ? value<=int.Parse(weight) :
                          operation==">" ? value>int.Parse(weight) :
                          operation=="<" ? value<int.Parse(weight) :
                          operation=="!=" ? value!=int.Parse(weight) : false;
            return result;
        }

        private static bool hero_trait_on_condition(Hero hero,string tag,string operation,string weight)
        {
            if(hero==null)
                return false;

            int value = GetHeroTraitValue(hero,tag);

            bool result = operation=="==" ? value==int.Parse(weight) :
                          operation==">=" ? value>=int.Parse(weight) :
                          operation=="<=" ? value<=int.Parse(weight) :
                          operation==">" ? value>int.Parse(weight) :
                          operation=="<" ? value<int.Parse(weight) :
                          operation=="!=" ? value!=int.Parse(weight) : false;
            return result;
        }

        private static int GetHeroTraitValue(Hero hero,string tag)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="conditions"></param>
        /// <param name="target"></param>
        /// <returns>1 if true,  0 if false, -1 if error;</returns>
        public static bool hero_trait_list_condition(Hero hero,string conditions, out string parseerror, params Hero [] target)
        {
            parseerror = "";

            if(string.IsNullOrEmpty(conditions))
            {
                /*parseerror="Error conditions are empty.";*/
                return false;
            }
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
                            parseerror="Error in equation: "+equationAND.ToString()+". Now will be using default cfg. Please fix or Delete cfg file.";
                            return false;
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
                        parseerror="Error in equation: "+equationItem.ToString()+". Now will be using default cfg. Please fix or Delete cfg file.";
                        return false;
                    }
                }

                result= result||ANDresult;
            }

            return result;
        }
    }
}
