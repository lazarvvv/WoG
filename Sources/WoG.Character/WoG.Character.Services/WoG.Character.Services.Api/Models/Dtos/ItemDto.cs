using WoG.Characters.Services.Api.Constants;
using ItemSuffixes = WoG.Characters.Services.Api.Constants.ItemConstants.Suffixes;

namespace WoG.Characters.Services.Api.Models.Dtos
{
    public class ItemDto
    {
        private string fullName = string.Empty;

        public Guid Id { get; set; }
        public required Guid OwnerId { get; set; }
        public required BaseItemDto BaseItem { get; set; }
        public int BonusStamina { get; set; }
        public int BonusStrength { get; set; }
        public int BonusAgility { get; set; }
        public int BonusIntelligence { get; set; }
        public int BonusFaith { get; set; }

        public string FullName
        {
            get
            {
                if (!string.IsNullOrEmpty(this.fullName))
                {
                    return this.fullName;
                }

                var (Stat, Suffix) = (BonusFaith, ItemSuffixes.FaithSuffix);

                if (Stat < this.BonusStamina)
                {
                    Stat = this.BonusStamina;
                    Suffix = ItemSuffixes.StaminaSuffix;
                }

                if (Stat < this.BonusStrength)
                {
                    Stat = this.BonusStrength;
                    Suffix = ItemSuffixes.StrengthSuffix;
                }

                if (Stat < this.BonusAgility)
                {
                    Stat = this.BonusAgility;
                    Suffix = ItemSuffixes.AgilitySuffix;
                }

                if (Stat < this.BonusIntelligence)
                {
                    Suffix = ItemSuffixes.IntelligenceSuffix;
                }

                this.fullName = $"{this.BaseItem.Name} {Suffix}";

                return this.fullName;
            }

            private set => this.fullName = value;
        }
    }
}
