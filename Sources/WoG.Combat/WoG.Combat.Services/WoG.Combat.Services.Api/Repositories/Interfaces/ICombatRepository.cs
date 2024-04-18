using Microsoft.AspNetCore.Mvc;
using WoG.Combat.Services.Api.Models.Dtos;

namespace WoG.Combat.Services.Api.Repositories.Interfaces
{
    public interface ICombatRepository
    {
        Task<DuelEventDto> DealDamage(Guid duelId, Guid characterId);
        Task<DuelEventDto> HealSelf(Guid duelId, Guid characterId);
        Task<DuelEventDto> CastSpell(Guid duelId, Guid characterId, Guid spellId);
        Task<DuelState> ComputeStateForDuel(Guid duelId, bool checkDb = false);
        Task<DuelDto> GetDuelFromCacheOrDb(Guid id, bool checkDb = true);
        Task CheckIfDuelExists(DuelRequestDto duelRequestDto);
        Task<bool> AddDuelToCache(DuelDto duelDto);
    }
}
