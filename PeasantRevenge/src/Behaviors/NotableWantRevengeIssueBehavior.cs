using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using static PeasantRevenge.Common;

namespace PeasantRevenge
{

    /// <summary>
    /// Issue for notable peasant in the village
    /// TODO: issue for town notable or governor.
    /// </summary>
    internal class NotableWantRevengeIssueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoadedEvent);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnGameLoadedEvent(CampaignGameStarter campaignGameStarter)
        {
            //TODO: Add 'start' dialogues for lords
        }
        // TODO: If raider is player, the quest should autostart, but now I do not know how to run multiple issues

        /* Checking issueGiver (Notable) if he will rebel. If it is true, the revenger party is created. Another check is for Revenger party leader.
         * If this Revenger party exists then condition holds for the revenge.
         */
        private bool ConditionsHold(Hero issueGiver)
        {
            if (issueGiver.PartyBelongedTo != null &&
               issueGiver.PartyBelongedTo.Name.Contains("Revenger"))
            {
                if (issueGiver.HomeSettlement != null &&
                    issueGiver.HomeSettlement.LastAttackerParty != null &&
                    issueGiver.HomeSettlement.IsVillage)
                {
                    System.Diagnostics.Debug.WriteLine($"Conditions hold for {issueGiver.Name.ToString()} of clan {issueGiver.Clan.Name.ToString()}");
                    return true;
                }
            }

            if (issueGiver.HomeSettlement != null &&
                issueGiver.HomeSettlement.IsVillage &&
                issueGiver.HomeSettlement.LastAttackerParty != null &&
                issueGiver.IsRuralNotable &&
                !CfgParser.hero_trait_list_condition(issueGiver, _cfg.values.peasantRevengerExcludeTrait, out string parseerror) &&
                issueGiver.HomeSettlement.Village.Bound.Town.Security <= 99f)
            {
                System.Diagnostics.Debug.WriteLine($"Conditions hold for {issueGiver.Name.ToString()} of {issueGiver.HomeSettlement.Name.ToString()}");

                if (issueGiver.HomeSettlement.Village != null)
                {
                    CreateNotableAssistantParty(issueGiver);
                }
                return false;
            }
            return false;
        }

        private void OnCheckForIssue(Hero hero)
        {
            if (this.ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(
                    new PotentialIssueData.StartIssueDelegate(this.OnStartIssue),
                    typeof(NotableWantRevengeIssue),
                    IssueBase.IssueFrequency.VeryCommon, null));
                return;
            }
            //Campaign.Current.IssueManager.AddPotentialIssueData(hero,new PotentialIssueData(
            //    typeof(NotableWantRevengeIssueBehavior.NotableWantRevengeIssue),
            //    IssueBase.IssueFrequency.VeryCommon));
        }

        private MobileParty CreateNotableAssistantParty(Hero QuestGiver)
        {
#if false
            string[] revenger_banners =
        {
            "31.116.145.1738.1518.768.788.1.0.0.113.116.116.390.335.771.811.0.1.0", /*chicken in the box*/
            "31.116.145.1738.1518.768.788.1.0.0.113.116.116.186.160.767.797.0.1.0.532.116.116.74.366.602.793.0.1.0.532.116.116.80.397.945.793.0.1.0", /*chicken in the box and two forks*/
            "31.116.145.1738.1518.768.788.1.0.0.124.116.116.186.160.767.797.0.1.0.532.116.116.74.366.602.793.0.1.0.532.116.116.80.397.945.793.0.1.0", /*horse in the box and two forks*/
            "31.116.145.1738.1518.768.788.1.0.0.149.116.116.186.160.767.797.0.0.0.532.116.116.74.366.602.793.0.1.0.532.116.116.80.397.945.793.0.1.0", /*boar in the box and two forks*/
            "31.116.145.1738.1518.768.788.1.0.0.505.116.116.186.160.767.881.0.0.0.308.116.116.257.215.779.708.0.0.155", /* cube nad fork*/
        };

            var production = QuestGiver.HomeSettlement.Village.VillageType.PrimaryProduction;
            int flag_symbol_index = 0;

            if (production.Name.ToString().ToLower().Contains("horse"))
            {
                flag_symbol_index = 2;
            }
            else if (production.Name.ToString().ToLower().Contains("salt"))
            {
                flag_symbol_index = 4;
            }
            else if (production.Name.ToString().ToLower().Contains("wool"))
            {
                flag_symbol_index = 2;
            }
            TextObject clanName = new TextObject($"{QuestGiver.Name}'s farm");

            Clan clan = Clan.CreateClan(clanName.ToString());
            var village_settlement = QuestGiver.HomeSettlement.Village.Settlement;

            lock (clan)
            {
                clan.Banner = new Banner(revenger_banners[flag_symbol_index]);
                clan.SetLeader(QuestGiver);
                clan.SetInitialHomeSettlement(village_settlement);
            }

            var culture = QuestGiver.Culture;

            List<CharacterObject> list = new List<CharacterObject>();
            foreach (CharacterObject characterObject in CharacterObject.All)
            {
                if (characterObject.Occupation == Occupation.Lord &&
                    characterObject.Culture == culture &&
                    characterObject.IsFemale == false
                    && characterObject.IsTemplate)
                {
                    list.Add(characterObject);
                }
            }

            if (list.IsEmpty())
            {
                return null;
            }

            CharacterObject charactertemplate = list.GetRandomElement();

            if (charactertemplate == null)
            {
                return null;
            }

            Hero _Revenger = HeroCreator.CreateSpecialHero(
                charactertemplate,
                village_settlement,
                clan,
                clan,
                MBRandom.RandomInt(18, 60));

            _Revenger.IsMinorFactionHero = true;
            _Revenger.ChangeHeroGold(Convert.ToInt32(village_settlement.Village.Gold));
            CharacterObject villager = QuestGiver.CharacterObject.Culture.Villager;
            EquipmentHelper.AssignHeroEquipmentFromEquipment(_Revenger, villager.Equipment);

            clan.SetLeader(_Revenger);
            clan.UpdateHomeSettlement(village_settlement);
            int size = (int)village_settlement.Village.Hearth >= _cfg.values.peasantRevengeMaxPartySize - 1 ?
                _cfg.values.peasantRevengeMaxPartySize - 1 : (int)village_settlement.Village.Hearth;
            var mp = clan.CreateNewMobilePartyAtPosition(_Revenger, village_settlement.Position2D);
            TextObject name = new TextObject("{=PRev0085}Revenger", null);
            mp.SetCustomName(name);
            mp.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"), size);
            mp.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("butter"), size);
            mp.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("cheese"), size);
            TroopRoster tr = new TroopRoster(mp.Party);
            tr.Clear();
            tr.AddToCounts(villager, size, false, 0, 0, true, -1);
            //tr.AddToCounts (base.QuestGiver.CharacterObject ,1 ,true ,0 ,0 ,true ,-1);          
            //mp.Party.SetCustomOwner (base.QuestGiver);
            mp.Ai.SetMovePatrolAroundSettlement(clan.HomeSettlement);
            _Revenger.ChangeState(Hero.CharacterStates.Active);
            mp.Party.SetVisualAsDirty();
            return mp;
#endif
            return null;
        }

        private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
        {
            return new NotableWantRevengeIssue(issueOwner);
        }

        public class NotableWantRevengeIssueTypeDefiner : SaveableTypeDefiner
        {
            public NotableWantRevengeIssueTypeDefiner() : base(808269866)
            {
            }

            protected override void DefineClassTypes()
            {
                base.AddClassDefinition(typeof(NotableWantRevengeIssue), 1, null);
                base.AddClassDefinition(typeof(NotableWantRevengeQuest), 2, null);
            }
        }
    }
}
