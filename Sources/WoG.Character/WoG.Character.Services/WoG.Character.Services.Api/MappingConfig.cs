using AutoMapper;
using WoG.Characters.Services.Api.Models;
using WoG.Characters.Services.Api.Models.Dtos;

namespace WoG.Characters.Services.Api
{
    public static class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            return new MapperConfiguration(config =>
            {
                config.AddProfile<CharacterClassMappingProfile>();
                config.AddProfile<CharacterMappingProfile>();
                config.AddProfile<BaseSpellMappingProfile>();
                config.AddProfile<SpellMappingProfile>();
                config.AddProfile<BaseItemMappingProfile>();
                config.AddProfile<ItemMappingProfile>();
            });
        }

        public class CharacterClassMappingProfile : Profile
        {
            public CharacterClassMappingProfile()
            {
                CreateMap<CharacterClass, CharacterClassDto>();
                CreateMap<CharacterClassDto, CharacterClass>();
            }
        }

        public class CharacterMappingProfile : Profile
        {
            public CharacterMappingProfile()
            {
                CreateMap<Character, CharacterDto>()
                    .ForMember(x => x.CharacterClass, opt => opt.MapFrom(src => src.CharacterClass))
                    .ForMember(x => x.Items, opt => opt.MapFrom(src => src.Items))
                    .ForMember(x => x.Spells, opt => opt.MapFrom(src => src.Spells));

                CreateMap<CharacterDto, Character>()
                    .ForMember(x => x.CharacterClass, opt => opt.MapFrom(src => src.CharacterClass))
                    .ForMember(x => x.Items, opt => opt.MapFrom(src => src.Items))
                    .ForMember(x => x.Spells, opt => opt.MapFrom(src => src.Spells));
            }
        }

        public class BaseSpellMappingProfile : Profile
        {
            public BaseSpellMappingProfile()
            {
                CreateMap<BaseSpell, BaseSpellDto>();
                CreateMap<BaseSpellDto, BaseSpell>();
            }
        }

        public class SpellMappingProfile : Profile
        {
            public SpellMappingProfile()
            {
                CreateMap<Spell, SpellDto>()
                    .ForMember(x => x.BaseSpell, opt => opt.MapFrom(src => src.BaseSpell));
                CreateMap<SpellDto, Spell>()
                    .ForMember(x => x.BaseSpell, opt => opt.MapFrom(src => src.BaseSpell));
            }
        }

        public class BaseItemMappingProfile : Profile
        {
            public BaseItemMappingProfile()
            {
                CreateMap<BaseItem, BaseItemDto>();
                CreateMap<BaseItemDto, BaseItem>();
            }
        }

        public class ItemMappingProfile : Profile
        {
            public ItemMappingProfile()
            {
                CreateMap<Item, ItemDto>()
                    .ForMember(x => x.BaseItem, opt => opt.MapFrom(src => src.BaseItem));
                CreateMap<ItemDto, Item>()
                    .ForMember(x => x.BaseItem, opt => opt.MapFrom(src => src.BaseItem));
            }
        }
    }
}
