using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace PeasantRevenge
{
    public class ReparationsBarterable : Barterable
    {
        public override string StringID
        {
            get
            {
                return "reparation_barterable";
            }
        }

        public ReparationsBarterable(Hero originalOwner, PartyBase originalParty, Hero heroProposedTo, Hero proposingHero, int value) : base(originalOwner, originalParty)
        {
            this.HeroProposedTo = heroProposedTo;
            this.ProposingHero = proposingHero;
            this.Reparation = value;
        }

        public override TextObject Name
        {
            get
            {
                StringHelpers.SetCharacterProperties("PROPOSING_HERO", this.ProposingHero.CharacterObject, null, false);
                return new TextObject("{=PRev0061}{PROPOSING_HERO.NAME} REPARATION " + Reparation.ToString(), null);
            }
        }

        public override void Apply()
        {
            if (HeroProposedTo != null)
            {
                GiveGoldAction.ApplyBetweenCharacters(ProposingHero, HeroProposedTo, (int)Reparation, true);
            }
        }

        public override int GetUnitValueForFaction(IFaction faction)
        {
            return -Reparation;
        }

        public override ImageIdentifier GetVisualIdentifier()
        {

            return null; //new ImageIdentifier(CharacterCode.CreateFrom(this.ProposingHero.CharacterObject));
          
        }

        public readonly Hero ProposingHero;
        public readonly Hero HeroProposedTo;       
        public int Reparation;
    }
}
