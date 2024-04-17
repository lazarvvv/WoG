namespace WoG.Characters.Services.Api.Constants
{
    public static class SeedingConstants
    {
        public static class Accounts
        {
            public static class Ids
            {
                public static readonly Guid Bob = Guid.Parse("32562268-20df-4e50-9255-3869f928e789");
                public static readonly Guid Karen = Guid.Parse("6f3dfb62-78e4-4ba2-886e-372830746fa6");
            }
        }

        public static class CharacterClasses
        {
            public static class Ids
            {
                public static readonly Guid Warrior = Guid.Parse("8534c080-e4a1-40bc-94fe-0277d5ace982");
                public static readonly Guid Bard = Guid.Parse("472ac03d-2d43-49fb-8570-dd37140f2a23");
                public static readonly Guid Rogue = Guid.Parse("cbd26795-0868-4eaf-8fea-7c8bb2bd7d60");
                public static readonly Guid Mage = Guid.Parse("d7cb583e-72d3-43fd-9f75-e15885f1ba6a");
            }

        }

        public static class BaseItems
        {
            public static class Names
            {
                public static readonly string Greatsword = "Greatsword";
                public static readonly string Longbow = "Longbow";
                public static readonly string Shank = "Shank";
                public static readonly string Greatstaff = "Greatstaff";
            }

            public static class Ids
            {
                public static readonly Guid Greatsword = Guid.Parse("d8aaedf4-6441-4550-a834-6f7d4d0c4132");
                public static readonly Guid Longbow = Guid.Parse("d30962de-89e1-4a25-92cc-ccffcb202966");
                public static readonly Guid Shank = Guid.Parse("fcb210e5-e63a-4c1b-b6c3-5040916eb2e7");
                public static readonly Guid Greatstaff = Guid.Parse("f4a27ee9-bab7-4834-a331-fbced1d48c2f");
            }

            public static class Stats
            {
                public static readonly int Greatsword = 20;
                public static readonly int Longbow = 18;
                public static readonly int Shank = 11;
                public static readonly int Greatstaff = 21;
            }
        }

        public static class Characters
        {
            public static class Ids
            {
                public static readonly Guid Bob = Guid.Parse("a5452df3-43e9-4440-aad8-5f2c62eebe57");
                public static readonly Guid Gorlock = Guid.Parse("a0b6969f-e3ff-4723-9427-5f10909c16d7");
                public static readonly Guid BobRogue = Guid.Parse("33d77148-d9ec-4772-a4e9-55980b8154b2");
            }

            public static class Names
            {
                public static readonly string Bob = "bob";
                public static readonly string Gorlock = "gorlock";
                public static readonly string BobRogue = "bobo";
            }
        }

        public static class BaseSpells
        {
            public static class Ids
            {
                public static readonly Guid BansheeWail = Guid.Parse("ea725616-19f5-4ccf-b006-b1fac18097f8");
                public static readonly Guid SongOfSerenity = Guid.Parse("8b71f08d-37ce-4a2a-a6a0-5a55310c68a8");
            }

            public static class Names
            {
                public static readonly string BansheeWail = "Banshee Wail";
                public static readonly string SongOfSerenity = "Flash Heal";
            }

            public static class Descriptions
            {
                public static readonly string BansheeWail = "Cries out with a supersonic voice, dealing damage to foes.";
                public static readonly string SongOfSerenity = "Plays a combination of notes on their magic lute, healing the injuries of allies.";
            }
        }
    }
}
