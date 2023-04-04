using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace PeasantRevenge
{
    public class GiftBarterable : Barterable
    {
        public override string StringID
        {
            get
            {
                return "reparation_barterable";
            }
        }

        public GiftBarterable(Hero originalOwner, PartyBase originalParty, Hero heroProposedTo, Hero proposingHero, int value) : base(originalOwner, originalParty)
        {
            this.HeroProposedTo = heroProposedTo;
            this.ProposingHero = proposingHero;
            this.Value = value;
        }

        public override TextObject Name
        {
            get
            {
                StringHelpers.SetCharacterProperties("PROPOSEDTO", this.HeroProposedTo.CharacterObject, null, false);
                return new TextObject("{=PRev0060}Gift to {PROPOSEDTO.NAME}", null);
            }
        }

        public override void Apply()
        {
            if (HeroProposedTo != null)
            {
                HeroProposedTo.Gold += Value;
                OriginalOwner.Gold -= Value;
            }
        }

        public override int GetUnitValueForFaction(IFaction faction)
        {
            return -Value;
        }

        public override ImageIdentifier GetVisualIdentifier()
        {

            return new ImageIdentifier((new ItemObject("gold")), "");

        }

        public readonly Hero ProposingHero;
        public readonly Hero HeroProposedTo;
        public int Value;
    }
}
