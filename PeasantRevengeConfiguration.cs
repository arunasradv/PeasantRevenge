//#define TESTING
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PeasantRevenge
{
#pragma warning disable IDE1006 // Naming Styles
    public class PeasantRevengeConfiguration
    {
        public int CfgVersion = 0;
        public bool enableRevengerMobileParty = false;
        public bool enableHelpNeutralVillageAndDeclareWarToAttackerMenu = false;
        public int ReparationsScaleToSettlementHearts = 30;
        public int relationChangeAfterReparationsReceived = 2;
        public int relationChangeWhenCannotPayReparations = -2;
        public int relationChangeWhenCriminalRefusedToPayReparations = -2;
        public int relationChangeWhenLordRefusedToPayReparations = -2;
        public int relationChangeWhenLordExecutedTheCriminal = 3;
        public int relationChangeWhenPlayerSavedTheCriminal = 3;
        public int relationLordAndCriminalChangeWhenLordSavedTheCriminal = -3;
        public int relationChangeWithCriminalClanWhenPlayerExecutedTheCriminal = -3;
        public int relationChangeWhenLordKilledMessenger = -3;
        public int relationChangeWhenLordRefusedToSupportPeasantRevenge = -2;
        public int relationChangeWhenLordTeachPeasant = 2;
        public int relationChangeLordAndCriminalWhenLordExecutedTheAccusedCriminal = 1;

        public int goldPercentOfPeasantTotallGoldToTeachPeasantToBeLoyal = 20;
        public bool alwaysExecuteTheCriminal = false;
        public bool alwaysLetLiveTheCriminal = false;
        public bool alowKingdomClanToSaveTheCriminal = true;
        public bool showPeasantRevengeLogMessages = true;
        public bool showPeasantRevengeLogMessagesForKingdom = true;
        public bool peasantRevengerIsRandom = false;
        public bool playerCanPayAnyKingdomClanReparations = true;
        public string peasantRevengerExcludeTrait = "Mercy > 0|Valor < 0";
        public string lordNotExecuteMessengerTrait = "Mercy > -1&Calculating > -1|Honor >= 1&Calculating > 0&Generosity > 0";
#if TESTING
        public string log_file_name = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log.xml");
#else
        public string log_file_name = "";// Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log.xml");
#endif
        public string file_name = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PeasantRevengeModCfg.xml");
        public int criminalHeroFromKingdomSuporterMinimumRelation = -70;
        public float criminalHeroFromKingdomSuporterMinimumAge = 18.0f;
        public int criminalHeroFromClanSuporterMinimumRelation = -70;
        public float criminalHeroFromClanSuporterMinimumAge = 18.0f;
        public int relationChangeAfterLordPartyGotPaid = 2;
        public int relationChangeAfterLordPartyGotNoReward = -2;
        public string lordWillAskRansomMoneyIfHasTraits = "Generosity < 1";
        public string lordWillOfferRansomMoneyIfHasTraits = "Generosity > -2&Relations > 0";
        public int lordWillOfferRansomMoneyWithProbabilityIfTraitFails = 10;
        public int lordWillDemandRansomMoneyIfHasLessGoldThan = 2000;
        public bool otherKingdomClanCanCareOfPeasantRevenge = true;
        public bool alwwaysReportPeasantRevengeToClanLeader = true;
        public float peasantRevengeTimeoutInDays = 5.0f;
        public float peasantRevengeSartTimeInDays = 0.2f;
        public int peasantRevengeMaxPartySize = 5;
        public bool allowLordToKillMessenger = true;
        public bool allowPeasantToKillLord = true;
#if TESTING
        public string logColorForClan = "hFF0000FF";
        public string logColorForKingdom = "hBB1111BB";
        public string logColorForOtherFactions = "hA02222A0";
#else
        public string logColorForClan = "hFFFFFFFF";
        public string logColorForKingdom = "hBBBBBBBB";
        public string logColorForOtherFactions = "hA0A0A0A0";
#endif

        public AIfilters ai;

        public class AIfilters
        {
            public List<RelationsPerTraits> partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet;
            public List<RelationsPerTraits> settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet;
#if false
        public List<RelationsPerTraits> partyLordWithTraitsAndRelationsWithSettlementClanWillContactLeader =
          new List<RelationsPerTraits>
          {
                new RelationsPerTraits {traits = "Generosity >= 0", relations = "Relations >= 0" },
          };
        public List<RelationsPerTraits> partyLordWithTraitsAndRelationsWithClanLeaderWillContactLeader =
          new List<RelationsPerTraits>
          {
                new RelationsPerTraits {traits = "Generosity >= 0", relations = "Relations >= 0" },
          };
        public List<RelationsPerTraits> ClanLeaderWithTraitsAndRelationsWithPartyCanPayReparation =
          new List<RelationsPerTraits>
          {
                new RelationsPerTraits {traits = "Generosity >= 0", relations = "Relations >= 0" },
          };
        public List<RelationsPerTraits> ClanLeaderWithTraitsAndRelationsWithPeasantCanPayReparation =
          new List<RelationsPerTraits>
          {
                new RelationsPerTraits {traits = "Generosity >= 0", relations = "Relations >= 0" },
          };
#endif

            public List<MoneyPerTraits> lordWillAffordPartOfHisSavingsToPayForFavor;
            public List<RelationsPerTraits> lordWillAffordToHelpTheCriminalAlly;
            public List<RelationsPerTraits> lordWillAffordToHelpTheCriminalEnemy;
            public List<RelationsPerTraits> lordWillAffordToHelpPayLostRansom;
            public List<RelationsPerTraits> lordIfFriendsWillHelpTheCriminal;
            public List<RelationsPerTraits> lordIfRelativesWillHelpTheCriminal;
            public List<RelationsPerTraits> criminalWillBlameOtherLordForTheCrime;
            public List<RelationsPerTraits> lordWillKillBothAccusedHeroAndCriminalLord;

            public void Default()
            {
              partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet = new List<RelationsPerTraits>
              {
                    new RelationsPerTraits { traits = "Generosity > 0&Calculating > 0&Mercy < 0&Valor > 0", relations = "Relations >= 10" },//loyal,impulsive,cruel or honorable,impulsive,loyal
              };

                settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet = new List<RelationsPerTraits>
              {
                    new RelationsPerTraits {traits = "Generosity > 0&Calculating > 0&Mercy < 0&Valor > 0", relations = "Relations >= 10" },//loyal,impulsive,cruel or honorable,impulsive,loyal
              };

                lordWillAffordPartOfHisSavingsToPayForFavor =
                new List<MoneyPerTraits>
                {
                new MoneyPerTraits {traits = "Generosity < -1", percent = 10 },
                new MoneyPerTraits {traits = "Generosity == -1", percent = 20 },
                new MoneyPerTraits {traits = "Generosity == 0", percent = 30 },
                new MoneyPerTraits {traits = "Generosity == 1", percent = 40 },
                new MoneyPerTraits {traits = "Generosity > 1", percent = 50 },
                }; 
                
                lordWillAffordToHelpTheCriminalEnemy =
                  new List<RelationsPerTraits>
                  {
                    new RelationsPerTraits {traits = "Mercy < -1&Honor < 1&Generosity < 1", relations = "Relations > 80" },
                    new RelationsPerTraits {traits = "Mercy == -1&Honor < 1&Generosity < 0", relations = "Relations > 70" },
                    new RelationsPerTraits {traits = "Mercy == 0&Honor < 1&Generosity < 0", relations = "Relations > 50" },
                    new RelationsPerTraits {traits = "Mercy == 1&Honor < 1&Generosity < 0", relations = "Relations > 25" },
                    new RelationsPerTraits {traits = "Mercy > 1&Honor < 0&Generosity < 0&Valor > 0", relations = "Relations > 10" },
                  };
                lordWillAffordToHelpTheCriminalAlly =
                  new List<RelationsPerTraits>
                  {
                    new RelationsPerTraits {traits = "Mercy < -1", relations = "Relations > 60" },
                    new RelationsPerTraits {traits = "Mercy == -1", relations = "Relations > 40" },
                    new RelationsPerTraits {traits = "Mercy == 0&Honor > -1", relations = "Relations > 20" },
                    new RelationsPerTraits {traits = "Mercy == 1&Honor > -1&Generosity > -1", relations = "Relations > 0" },
                    new RelationsPerTraits {traits = "Mercy > 1&Honor > 0&Generosity > 0", relations = "Relations > -10" },
                  };
                lordWillAffordToHelpPayLostRansom =
                  new List<RelationsPerTraits>
                  {
                    new RelationsPerTraits {traits = "Mercy < -1|Generosity < -1", relations = "Relations > 20" },
                    new RelationsPerTraits {traits = "Mercy == -1|Generosity == -1", relations = "Relations > 10" },
                    new RelationsPerTraits {traits = "Mercy == 0|Generosity == 0", relations = "Relations > 0" },
                    new RelationsPerTraits {traits = "Mercy == 1|Generosity == 1&Honor > 0", relations = "Relations > -10" },
                    new RelationsPerTraits {traits = "Mercy > 1|Generosity > 1&Honor > 0", relations = "Relations > -20" },
                  };

                lordIfFriendsWillHelpTheCriminal =
                 new List<RelationsPerTraits>
                 {
                new RelationsPerTraits {traits = "Mercy < -1", relations = "Relations > 30" },
                new RelationsPerTraits {traits = "Mercy == -1", relations = "Relations > 20" },
                new RelationsPerTraits {traits = "Mercy == 0", relations = "Relations > 0"},
                new RelationsPerTraits {traits = "Mercy == 1&Honor > 0", relations = "Relations > -20"},
                new RelationsPerTraits {traits = "Mercy > 1&Honor > 0&Generosity > 0", relations =  "Relations > -30"},
                 };
                lordIfRelativesWillHelpTheCriminal =
                  new List<RelationsPerTraits>
                  {
                new RelationsPerTraits {traits = "Mercy < -1", relations = "Relations > 20" },
                new RelationsPerTraits {traits = "Mercy == -1", relations = "Relations > 10" },
                new RelationsPerTraits {traits = "Mercy == 0", relations = "Relations > -30"},
                new RelationsPerTraits {traits = "Mercy == 1&Honor > 0", relations = "Relations > -50"},
                new RelationsPerTraits {traits = "Mercy > 1&Honor > 0&Generosity > 0", relations =  "Relations > -70"},
                  };
                default_lordWillKillBothAccusedHeroAndCriminalLord();
                default_criminalWillBlameOtherLordForTheCrime();
            }

            public void default_lordWillKillBothAccusedHeroAndCriminalLord()
            {
                lordWillKillBothAccusedHeroAndCriminalLord =
                 new List<RelationsPerTraits>
                 {
                    new RelationsPerTraits {traits = "Mercy < 0", relations = "Relations < 0" },
                 };
            }

            public void default_criminalWillBlameOtherLordForTheCrime()
            {
                criminalWillBlameOtherLordForTheCrime =
                 new List<RelationsPerTraits>
                 {
                   //passive dependent
                    new RelationsPerTraits {traits = "Mercy < 0&Honor < 1&Generosity < 1&Calculating < 0&Valor <= 0", relations = "Relations < 10"},
                   //dominant manipalutive
                    new RelationsPerTraits {traits = "Mercy < 0&Honor < 1&Generosity < 1&Calculating > 0&Valor >= 0", relations =  "Relations < 10"},
                 };
            }

        }

        //"Mercy represents your general aversion to suffering and your willingness to help strangers or even enemies."
        //"Valor represents your reputation for risking your life to win glory or wealth or advance your cause."
        //"Honor represents your reputation for respecting your formal commitments, like keeping your word and obeying the law."
        //"Generosity represents your loyalty to your kin and those who serve you, and your gratitude to those who have done you a favor."
        //"Calculating represents your ability to control your emotions for the sake of your long-term interests."

        public class MoneyPerTraits
        {
            [XmlAttribute]
            public string traits = "";
            [XmlAttribute]
            public int percent = 0;
        }

        public class RelationsPerTraits
        {
            [XmlAttribute]
            public string traits = "";
            [XmlAttribute]
            public string relations = "";
        }


        // public bool prisonerAILordCannotRansomTheCriminal = true;
        // public bool raidedSettlementOwnerCanInfluencePeasantRevenge = true;
    }
}
