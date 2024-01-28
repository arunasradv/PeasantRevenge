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
        public int CfgVersion = 21;
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
        public int relationChangeWhenLordBribePeasant = 2;
        public int relationChangeLordAndCriminalWhenLordExecutedTheAccusedCriminal = 1;
        public int relationChangeWhenLordDeclinedRansomOfferForCriminalLordRemains = -10;
        public int goldPercentOfPeasantTotallGoldToTeachPeasantToBeLoyal = 20;
        public bool alwaysExecuteTheCriminal = false;
        public bool alwaysLetLiveTheCriminal = false;
        public bool alowKingdomClanToSaveTheCriminal = true;
        public bool showPeasantRevengeLogMessages = true;
        public bool showPeasantRevengeLogMessagesForKingdom = true;
        public bool peasantRevengerIsRandom = false;
        public bool playerCanPayAnyKingdomClanReparations = true;
        public string peasantRevengerExcludeTrait = "Mercy >= 0|Valor =< 0";
        public string lordNotExecuteMessengerTrait = "Mercy > -1&Calculating > -1|Honor >= 1&Calculating > 0&Generosity > 0";
#if TESTING
        public string log_file_name = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log.xml");
#else
        public string log_file_name = "";// Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log.xml");
#endif
        public string file_name = default_file_name();
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
        public float peasantRevengePartyTalkToLordDistance = 2.0f;
        public float peasantRevengePartyWaitLordDistance = 0.5f;
        public bool allowLordToKillMessenger = true;
        public bool allowPeasantToKillLord = true;
        public string logColorForClan = "hFF0000FF";
        public string logColorForKingdom = "hBB1111BB";
        public string logColorForOtherFactions = "hA02222A0";
        public float peasantRevengerIntimidationPowerScale = 0.5f;
        public bool enableOtherNobleTraitsChangeAfterNobleExecution = true;
        public int lordCanTryAsManyTimesToPersuadeTheNotable = 5;
        public AIfilters ai;

        public static string default_file_name()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PeasantRevengeModCfg.xml");
        }

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
            public List<TraitAndValue> lordTraitChangeWhenRansomRemainsDeclined;
            public List<RelationsPerTraits> lordWillNotKillBothAccusedHeroAndCriminalLordDueConflict;
            public List<TraitAndValue> lordTraitChangeWhenRansomRemainsAccepted;
            public List<TraitAndValue> lordTraitChangeWhenRemainsOfLordAreAbandoned;
            public List<RelationsPerTraits> lordWillDeclineRansomTheVictimRemains;
            public List<RelationsPerTraits> lordWillAbandonTheVictimRemains;
            public List<TraitAndValue> lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest;
            public List<TraitAndValue> lordTraitChangeWhenLordPersuedeNotableNotToRevenge;
            public List<TraitAndValue> lordTraitChangeWhenLordPersuedeNotableToRevenge;
            public List<RelationsPerTraits> notableWillAcceptTheBribe;
            public List<TraitAndValue> PersuadeNotableToRevengeTraitsForOption0;
            public List<TraitAndValue> PersuadeNotableToRevengeTraitsForOption1;
            public List<TraitAndValue> PersuadeNotableToRevengeTraitsForOption2;
            public List<TraitAndValue> PersuadeNotableNotToRevengeTraitsForOption0;
            public List<TraitAndValue> PersuadeNotableNotToRevengeTraitsForOption1;
            public List<TraitAndValue> PersuadeNotableNotToRevengeTraitsForOption2;
            public List<TraitAndValue> AccuseNotableTraitsForOption0;
            public List<TraitAndValue> AccuseNotableTraitsForOption1;
            public List<TraitAndValue> AccuseNotableTraitsForOption2;
            public List<RelationsPerTraits> lordPersuadeNotableExcludeTraitsAndRelations;
            public void Default()
            {
                default_partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet();
                default_settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet();
                default_lordWillAffordPartOfHisSavingsToPayForFavor();
                default_lordWillAffordToHelpTheCriminalEnemy();
                default_lordWillAffordToHelpTheCriminalAlly();
                default_lordWillAffordToHelpPayLostRansom();
                default_lordIfFriendsWillHelpTheCriminal();
                default_lordIfRelativesWillHelpTheCriminal();
                default_notableWillAcceptTheBribe();
                default_lordWillKillBothAccusedHeroAndCriminalLord();
                default_criminalWillBlameOtherLordForTheCrime();
                default_lordTraitChangeWhenRansomRemainsDeclined();
                default_lordTraitChangeWhenRansomRemainsAccepted();
                default_lordTraitChangeWhenRemainsOfLordAreAbandoned();
                default_lordWillDeclineRansomTheVictimRemains();
                default_lordWillAbandonTheVictimRemains();
                default_lordWillNotKillBothAccusedHeroAndCriminalLordDueConflict();
                default_lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest();
                default_lordTraitChangeWhenLordPersuedeNotableNotToRevenge();
                default_lordTraitChangeWhenLordPersuedeNotableToRevenge();
                default_PersuadeNotableToRevengeTraitsForOption0();
                default_PersuadeNotableToRevengeTraitsForOption1();
                default_PersuadeNotableToRevengeTraitsForOption2();
                default_PersuadeNotableNotToRevengeTraitsForOption0();
                default_PersuadeNotableNotToRevengeTraitsForOption1();
                default_PersuadeNotableNotToRevengeTraitsForOption2();
                default_AccuseNotableTraitsForOption0();
                default_AccuseNotableTraitsForOption1();
                default_AccuseNotableTraitsForOption2();
                default_lordPersuadeNotableExcludeTraitsAndRelations();
        }

            public void default_partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet()
            {
                partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet = new List<RelationsPerTraits>
              {
                    new RelationsPerTraits { traits = "Generosity > 0&Calculating > 0&Mercy < 0&Valor > 0", relations = "Relations >= 10" },//loyal,impulsive,cruel or honorable,impulsive,loyal
              };
            }
            public void default_settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet()
            {
             settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet = new List<RelationsPerTraits>
              {
                    new RelationsPerTraits {traits = "Generosity > 0&Calculating > 0&Mercy < 0&Valor > 0", relations = "Relations >= 10" },//loyal,impulsive,cruel or honorable,impulsive,loyal
              };
            }
            public void default_lordWillAffordPartOfHisSavingsToPayForFavor()
            {
                lordWillAffordPartOfHisSavingsToPayForFavor =
                new List<MoneyPerTraits>
                {
                new MoneyPerTraits {traits = "Generosity < -1", percent = 10 },
                new MoneyPerTraits {traits = "Generosity == -1", percent = 20 },
                new MoneyPerTraits {traits = "Generosity == 0", percent = 30 },
                new MoneyPerTraits {traits = "Generosity == 1", percent = 40 },
                new MoneyPerTraits {traits = "Generosity > 1", percent = 50 },
                };
            }
            public void default_lordWillAffordToHelpTheCriminalEnemy()
            {
                lordWillAffordToHelpTheCriminalEnemy =
                  new List<RelationsPerTraits>
                  {
                    new RelationsPerTraits {traits = "Mercy < -1&Honor < 1&Generosity < 1", relations = "Relations > 80" },
                    new RelationsPerTraits {traits = "Mercy == -1&Honor < 1&Generosity < 0", relations = "Relations > 70" },
                    new RelationsPerTraits {traits = "Mercy == 0&Honor < 1&Generosity < 0", relations = "Relations > 50" },
                    new RelationsPerTraits {traits = "Mercy == 1&Honor < 1&Generosity < 0", relations = "Relations > 25" },
                    new RelationsPerTraits {traits = "Mercy > 1&Honor < 0&Generosity < 0&Valor > 0", relations = "Relations > 10" },
                  };
            }
            public void default_lordWillAffordToHelpTheCriminalAlly()
            {
                lordWillAffordToHelpTheCriminalAlly =
                  new List<RelationsPerTraits>
                  {
                    new RelationsPerTraits {traits = "Mercy < -1", relations = "Relations > 60" },
                    new RelationsPerTraits {traits = "Mercy == -1", relations = "Relations > 40" },
                    new RelationsPerTraits {traits = "Mercy == 0&Honor > -1", relations = "Relations > 20" },
                    new RelationsPerTraits {traits = "Mercy == 1&Honor > -1&Generosity > -1", relations = "Relations > 0" },
                    new RelationsPerTraits {traits = "Mercy > 1&Honor > 0&Generosity > 0", relations = "Relations > -10" },
                  };
            }
            public void default_lordWillAffordToHelpPayLostRansom()
            {
                 lordWillAffordToHelpPayLostRansom =
                  new List<RelationsPerTraits>
                  {
                    new RelationsPerTraits {traits = "Mercy < -1|Generosity < -1", relations = "Relations > 20" },
                    new RelationsPerTraits {traits = "Mercy == -1|Generosity == -1", relations = "Relations > 10" },
                    new RelationsPerTraits {traits = "Mercy == 0|Generosity == 0", relations = "Relations > 0" },
                    new RelationsPerTraits {traits = "Mercy == 1|Generosity == 1&Honor > 0", relations = "Relations > -10" },
                    new RelationsPerTraits {traits = "Mercy > 1|Generosity > 1&Honor > 0", relations = "Relations > -20" },
                  };
            }
            public void default_lordIfFriendsWillHelpTheCriminal() { 
                lordIfFriendsWillHelpTheCriminal =
                 new List<RelationsPerTraits>
                 {
                new RelationsPerTraits {traits = "Mercy < -1", relations = "Relations > 30" },
                new RelationsPerTraits {traits = "Mercy == -1", relations = "Relations > 20" },
                new RelationsPerTraits {traits = "Mercy == 0", relations = "Relations > 0"},
                new RelationsPerTraits { traits = "Mercy == 1&Honor > 0", relations = "Relations > -20" },
                new RelationsPerTraits { traits = "Mercy > 1&Honor > 0&Generosity > 0", relations = "Relations > -30" },
                 };
            }
            public void default_lordIfRelativesWillHelpTheCriminal()
            {
                lordIfRelativesWillHelpTheCriminal =
                  new List<RelationsPerTraits>
                  {
                new RelationsPerTraits {traits = "Mercy < -1", relations = "Relations > 20" },
                new RelationsPerTraits {traits = "Mercy == -1", relations = "Relations > 10" },
                new RelationsPerTraits {traits = "Mercy == 0", relations = "Relations > -30"},
                new RelationsPerTraits {traits = "Mercy == 1&Honor > 0", relations = "Relations > -50"},
                new RelationsPerTraits {traits = "Mercy > 1&Honor > 0&Generosity > 0", relations =  "Relations > -70"},
                  };
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

            public void default_lordTraitChangeWhenRansomRemainsDeclined()
            {
                lordTraitChangeWhenRansomRemainsDeclined =
                    new List<TraitAndValue>
                    {
                        new TraitAndValue { trait = "Mercy", value = -2},
                        new TraitAndValue { trait = "Honor", value = -5},
                        new TraitAndValue { trait = "Calculating", value = -2},
                        new TraitAndValue { trait = "Valor", value = 1},
                    };
            }

            public void default_lordTraitChangeWhenRansomRemainsAccepted()
            {
                lordTraitChangeWhenRansomRemainsAccepted =
                    new List<TraitAndValue>
                    {
                        new TraitAndValue { trait = "Mercy", value = 2},
                        new TraitAndValue { trait = "Honor", value = 5},
                        new TraitAndValue { trait = "Calculating", value = 2},
                        new TraitAndValue { trait = "Valor", value = 1},
                    };
            }

            public void default_lordTraitChangeWhenRemainsOfLordAreAbandoned()
            {
                lordTraitChangeWhenRemainsOfLordAreAbandoned =
                    new List<TraitAndValue>
                    {
                        new TraitAndValue { trait = "Mercy", value = -2},
                        new TraitAndValue { trait = "Honor", value = -5},
                        new TraitAndValue { trait = "Calculating", value = -2},
                        new TraitAndValue { trait = "Valor", value = -1},
                    };
            }

            
            public void default_lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest()
            {
                lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest =
                    new List<TraitAndValue>
                    {
                        new TraitAndValue { trait = "Mercy", value = -2},
                        new TraitAndValue { trait = "Honor", value = -3}
                    };
            }
            public void default_lordWillDeclineRansomTheVictimRemains()
            {
                lordWillDeclineRansomTheVictimRemains =
                 new List<RelationsPerTraits>
                 {
                    new RelationsPerTraits {traits = "Honor < 0&Calculating < 0&Valor > 0", relations = "Relations < -50"}
                 };
            }

            public void default_lordWillAbandonTheVictimRemains()
            {
                lordWillAbandonTheVictimRemains =
                 new List<RelationsPerTraits>
                 {
                    new RelationsPerTraits {traits = "Honor < 0&Calculating < 0", relations = "Relations < -50"},
                 };
            }

            public void default_lordWillNotKillBothAccusedHeroAndCriminalLordDueConflict()
            {
              lordWillNotKillBothAccusedHeroAndCriminalLordDueConflict = new List<RelationsPerTraits>
                  {
                    new RelationsPerTraits {traits = "Mercy == 0", relations = "Relations > 20" },
                    new RelationsPerTraits {traits = "Mercy > 0", relations = "Relations > 0" },
                  };
            }
            public void default_lordTraitChangeWhenLordPersuedeNotableNotToRevenge()
            {
                    lordTraitChangeWhenLordPersuedeNotableNotToRevenge =
                    new List<TraitAndValue>
                    {
                        new TraitAndValue { trait = "Mercy", value = 5},
                        new TraitAndValue { trait = "Valor", value = -5},
                    };
            }
            public void default_lordTraitChangeWhenLordPersuedeNotableToRevenge()
            {
                lordTraitChangeWhenLordPersuedeNotableToRevenge =
                    new List<TraitAndValue>
                    {
                        new TraitAndValue { trait = "Mercy", value = -5},
                        new TraitAndValue { trait = "Valor", value = 5},
                    };
            }
            public void default_notableWillAcceptTheBribe()
            {
                notableWillAcceptTheBribe =
                new List<RelationsPerTraits>
                {
                     new RelationsPerTraits {traits = "Honor < 0&Generosity >= 0", relations = "Relations >= -10" },
                     new RelationsPerTraits {traits = "Honor == 0&Generosity >= 0", relations = "Relations >= 10" },
                     new RelationsPerTraits {traits = "Honor > 0&Generosity >= 0", relations = "Relations >= 20" },
                };
            }

            public void default_PersuadeNotableToRevengeTraitsForOption0()
            {
                PersuadeNotableToRevengeTraitsForOption0 =
                   new List<TraitAndValue>
                   {
                        new TraitAndValue { trait = "Mercy", value = -1},
                        new TraitAndValue { trait = "Honor", value = 0},
                        new TraitAndValue { trait = "Calculating", value = -1},
                        new TraitAndValue { trait = "Valor", value = 1},
                        new TraitAndValue { trait = "Generosity", value = 1}
                   };
            }

            public void default_PersuadeNotableToRevengeTraitsForOption1()
            {
                PersuadeNotableToRevengeTraitsForOption1=
                   new List<TraitAndValue>
                   {
                        new TraitAndValue { trait = "Mercy", value = -1},
                        new TraitAndValue { trait = "Honor", value = 0},
                        new TraitAndValue { trait = "Calculating", value = -1},
                        new TraitAndValue { trait = "Valor", value = 1},
                        new TraitAndValue { trait = "Generosity", value = 1}
                   };
            }

            public void default_PersuadeNotableToRevengeTraitsForOption2()
            {
                PersuadeNotableToRevengeTraitsForOption2=
                   new List<TraitAndValue>
                   {
                        new TraitAndValue { trait = "Mercy", value = 0},
                        new TraitAndValue { trait = "Honor", value = -1},
                        new TraitAndValue { trait = "Calculating", value = -1},
                        new TraitAndValue { trait = "Valor", value = 1},
                        new TraitAndValue { trait = "Generosity", value = -1}
                   };
            }

            public void default_PersuadeNotableNotToRevengeTraitsForOption0()
            {
                PersuadeNotableNotToRevengeTraitsForOption0=
                   new List<TraitAndValue>
                   {
                        new TraitAndValue { trait = "Mercy", value = 1},
                        new TraitAndValue { trait = "Honor", value = 0},
                        new TraitAndValue { trait = "Calculating", value = 1},
                        new TraitAndValue { trait = "Valor", value = -1},
                        new TraitAndValue { trait = "Generosity", value = 0}
                   };
            }
            public void default_PersuadeNotableNotToRevengeTraitsForOption1()
            {
                PersuadeNotableNotToRevengeTraitsForOption1=
                   new List<TraitAndValue>
                   {
                        new TraitAndValue { trait = "Mercy", value = 1},
                        new TraitAndValue { trait = "Honor", value = 0},
                        new TraitAndValue { trait = "Calculating", value = 1},
                        new TraitAndValue { trait = "Valor", value = -1},
                        new TraitAndValue { trait = "Generosity", value = -1}
                   };
            }

            public void default_PersuadeNotableNotToRevengeTraitsForOption2()
            {
                PersuadeNotableNotToRevengeTraitsForOption2=
                   new List<TraitAndValue>
                   {
                        new TraitAndValue { trait = "Mercy", value = 1},
                        new TraitAndValue { trait = "Honor", value = 0},
                        new TraitAndValue { trait = "Calculating", value = 1},
                        new TraitAndValue { trait = "Valor", value = -1},
                        new TraitAndValue { trait = "Generosity", value = 1}
                   };
            }

            public void default_AccuseNotableTraitsForOption0()
            {
                AccuseNotableTraitsForOption0=
                   new List<TraitAndValue>
                   {
                        new TraitAndValue { trait = "Mercy", value = -1},
                        new TraitAndValue { trait = "Honor", value = -1},
                        new TraitAndValue { trait = "Calculating", value = -1},
                        new TraitAndValue { trait = "Valor", value = 1},
                        new TraitAndValue { trait = "Generosity", value = -1}
                   };
            }

            public void default_AccuseNotableTraitsForOption1()
            {
                AccuseNotableTraitsForOption1=
                   new List<TraitAndValue>
                   {
                        new TraitAndValue { trait = "Mercy", value = 1},
                        new TraitAndValue { trait = "Honor", value = 0},
                        new TraitAndValue { trait = "Calculating", value = 0},
                        new TraitAndValue { trait = "Valor", value = -1},
                        new TraitAndValue { trait = "Generosity", value = -1}
                   };
            }

            public void default_AccuseNotableTraitsForOption2()
            {
                AccuseNotableTraitsForOption2=
                   new List<TraitAndValue>
                   {
                        new TraitAndValue { trait = "Mercy", value = 0},
                        new TraitAndValue { trait = "Honor", value = 1},
                        new TraitAndValue { trait = "Calculating", value = 1},
                        new TraitAndValue { trait = "Valor", value = 0},
                        new TraitAndValue { trait = "Generosity", value = 1}
                   };
            }

            public void default_lordPersuadeNotableExcludeTraitsAndRelations()
            {
                lordPersuadeNotableExcludeTraitsAndRelations=new List<RelationsPerTraits>
                {
                     new RelationsPerTraits {traits = "Calculating == 0", relations = "Relations < -20" }
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
        public class TraitAndValue
        {
            [XmlAttribute]
            public string trait = "";
            [XmlAttribute]
            public int value = 0;
        }

        public class TraitsAndValue
        {
            [XmlAttribute]
            public string traits = "";
            [XmlAttribute]
            public int percent = 0;
        }

        // public bool prisonerAILordCannotRansomTheCriminal = true;
        // public bool raidedSettlementOwnerCanInfluencePeasantRevenge = true;
    }
}
