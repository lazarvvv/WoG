using AutoMapper;
using WoG.Combat.Services.Api.Models;
using WoG.Combat.Services.Api.Models.Dtos;

namespace WoG.Combat.Service.Api
{
    public static class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            return new MapperConfiguration(config =>
            {
                config.AddProfile<DuelMappingProfile>();
                config.AddProfile<DuelEventMappingProfile>();
            });
        }

        public class DuelMappingProfile : Profile
        {
            public DuelMappingProfile()
            {
                CreateMap<Duel, DuelDto>()
                    .ForMember(x => x.Events, opt => opt.MapFrom(src => src.Events));
                CreateMap<DuelDto, Duel>()
                    .ForMember(x => x.Events, opt => opt.MapFrom(src => src.Events));
            }
        }

        public class DuelEventMappingProfile : Profile
        {
            public DuelEventMappingProfile()
            {
                CreateMap<DuelEvent, DuelEventDto>();
                CreateMap<DuelEventDto, DuelEvent>();
            }
        }
    }
}
