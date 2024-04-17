namespace WoG.Characters.Services.Api.Models.Dtos
{
    public class CharacterDto
    {
        //Base Properties
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required CharacterClassDto CharacterClass { get; set; }
        public Guid CreatedBy { get; set; }
        public IEnumerable<ItemDto> Items { get; set; } = [];
        public IEnumerable<SpellDto> Spells { get; set; } = [];
        public long Experience { get; set; }
        public int BonusStamina { get; set; }
        public int BonusStrength { get; set; }
        public int BonusAgility { get; set; }
        public int BonusIntelligence { get; set; }
        public int BonusFaith { get; set; }

        //Calculated Properties
        public string FullName => $"{this.Name} The {this.CharacterClass.Name}";
        public long Level { get => this.Experience / 200; }
        public long LeftoverExperience { get => this.Experience % 200; }

        public long SkillPoints 
        { 
            get => (this.Level * 2) - this.BonusStrength - this.BonusAgility - this.BonusIntelligence - this.BonusFaith; 
        }

        //CHECK: Still on the fence on whether or not to calculate this, or let the combat service do this too
        public int Health { get => this.Stamina * 20; }
        public int Mana { get => this.Intelligence * 10; }

        public int BaseStamina { get => this.CharacterClass.BaseStamina; }
        public int BaseStrength { get => this.CharacterClass.BaseStrength; }
        public int BaseAgility { get => this.CharacterClass.BaseAgility; }
        public int BaseIntelligence { get => this.CharacterClass.BaseIntelligence; }
        public int BaseFaith { get => this.CharacterClass.BaseFaith; }

        public int Stamina { get => this.BaseStamina + this.BonusStamina + this.Items.Sum(x => x.BonusStamina); }
        public int Strength { get => this.BaseStrength + this.BonusStrength + this.Items.Sum(x => x.BonusStrength); }
        public int Agility { get => this.BaseAgility + this.BonusAgility + this.Items.Sum(x => x.BonusAgility); }
        public int Intelligence { get => this.BaseIntelligence + this.BonusIntelligence + this.Items.Sum(x => x.BonusIntelligence); }
        public int Faith { get => this.BaseFaith + this.BonusFaith + this.Items.Sum(x => x.BonusFaith); }
    }
}
