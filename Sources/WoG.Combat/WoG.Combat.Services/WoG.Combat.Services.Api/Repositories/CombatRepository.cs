using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;
using WoG.Combat.Services.Api.Data;
using WoG.Combat.Services.Api.Exceptions;
using WoG.Combat.Services.Api.Models;
using WoG.Combat.Services.Api.Models.Dtos;
using WoG.Combat.Services.Api.Repositories.Interfaces;

namespace WoG.Combat.Services.Api.Repositories
{
    public class CombatRepository(ApplicationDbContext dbContext, IConnectionMultiplexer redisConnectionMultiplexer,
        ILogger<CombatRepository> logger, IMapper mapper, IConfiguration appSettings) : ICombatRepository
    {
        private readonly ApplicationDbContext dbContext = dbContext;

        private readonly int damageCooldownInSeconds = appSettings.GetValue<int>("CombatProperties:DamageCooldownInSeconds");
        private readonly int healingCooldownInSeconds = appSettings.GetValue<int>("CombatProperties:HealingCooldownInSeconds");
        private readonly int maximumDuelDurationInSeconds = appSettings.GetValue<int>("CombatProperties:MaximumDuelDurationInSeconds");

        private readonly IDatabase redisDatabase = redisConnectionMultiplexer.GetDatabase();
        private readonly ILogger<CombatRepository> logger = logger;
        private readonly IMapper mapper = mapper;

        public async Task<DuelEventDto> CastSpell(Guid duelId, Guid characterId, Guid spellId)
        {
            var duelDto = await GetDuelFromCacheOrDb(duelId, false);

            if (duelDto == null)
            {
                throw new Status400Exception("CastSpell :: Duel with id doesn't exist or is already finished.");
            }

            var spells = await GetSpellsFromCacheOrDbForCharacter(characterId);
            var spell = spells.FirstOrDefault(x => x.Id == spellId);

            if (spell == null)
            {
                throw new Status400Exception($"CastSpell :: Spell with Id = {spellId} isn't known by character with id = {characterId}.");
            }

            var duelState = await ComputeStateForDuel(duelId);

            if (IsDuelADraw(duelState))
            {
                duelState.DuelOutcome = Enums.DuelOutcome.Draw;

                await PersistDuelInfo(duelState);

                throw new Status400Exception($"The time has run out, the duel ended with a draw.");
            }

            var sequence = duelDto.Events.Max(x => x.Sequence) + 1;

            if (spell.Damage > 0 && duelState.Challenger.Id == characterId && (duelState.Defender.Health - spell.Damage) <= 0)
            {
                duelState.Defender.Health = 0;
                duelState.DuelOutcome = Enums.DuelOutcome.ChallengerWon;

                var challengerWonEvent = new DuelEventDto
                {
                    DuelId = duelState.Id,
                    Sequence = sequence,
                    CharacterId = characterId,
                    EventType = Enums.EventType.SpellCast,
                    TimeWhenNextActionOfTypeAvailable = DateTime.Now.AddSeconds(this.damageCooldownInSeconds),
                    Damage = spell.Damage,
                    Healing = duelState.Challenger.Health + spell.Healing > duelState.Challenger.InitialHealth ?
                        duelState.Challenger.InitialHealth - duelState.Challenger.Health :
                        spell.Healing
                };

                duelDto.Events.Add(challengerWonEvent);

                await PersistDuelInfo(duelState);
                return challengerWonEvent;
            }

            if (spell.Damage > 0 && duelState.Defender.Id == characterId && (duelState.Challenger.Health - spell.Damage) <= 0)
            {
                duelState.Challenger.Health = 0;
                duelState.DuelOutcome = Enums.DuelOutcome.DefenderWon;

                var defenderWonEvent = new DuelEventDto
                {
                    DuelId = duelState.Id,
                    Sequence = sequence,
                    CharacterId = characterId,
                    EventType = Enums.EventType.SpellCast,
                    TimeWhenNextActionOfTypeAvailable = DateTime.Now.AddSeconds(this.damageCooldownInSeconds),
                    Damage = spell.Damage,
                    Healing = duelState.Challenger.Health + spell.Healing > duelState.Challenger.InitialHealth ?
                        duelState.Challenger.InitialHealth - duelState.Challenger.Health :
                        spell.Healing
                };

                duelDto.Events.Add(defenderWonEvent);

                await PersistDuelInfo(duelState);
                return defenderWonEvent;
            }

            var @event = new DuelEventDto
            {
                DuelId = duelState.Id,
                Sequence = sequence,
                CharacterId = characterId,
                EventType = Enums.EventType.DamageDealt,
                TimeWhenNextActionOfTypeAvailable = DateTime.Now.AddSeconds(this.damageCooldownInSeconds),
                Damage = spell.Damage,
                Healing = duelState.Challenger.Health + spell.Healing > duelState.Challenger.InitialHealth ?
                        duelState.Challenger.InitialHealth - duelState.Challenger.Health :
                        spell.Healing
            };

            await AddDuelToCache(duelDto);

            duelDto.Events.Add(@event);

            return @event;
        }


        public async Task<DuelEventDto> DealDamage(Guid duelId, Guid characterId)
        {
            var duelDto = await GetDuelFromCacheOrDb(duelId, false) ?? 
                throw new Status400Exception("DealDamage :: Duel with id doesn't exist or is already finished.");
            
            var duelState = await ComputeStateForDuel(duelId);
            var damageDealt = characterId == duelState.Challenger.Id ? duelState.Challenger.Damage : duelState.Defender.Damage;

            if (IsDuelADraw(duelState))
            {
                duelState.DuelOutcome = Enums.DuelOutcome.Draw;

                await PersistDuelInfo(duelState);

                throw new Status400Exception($"The time has run out, the duel ended with a draw.");
            }

            var sequence = duelDto.Events.Max(x => x.Sequence) + 1;

            if (duelState.Challenger.Id == characterId && (duelState.Defender.Health - damageDealt) <= 0)
            {
                duelState.Defender.Health = 0;
                duelState.DuelOutcome = Enums.DuelOutcome.ChallengerWon;

                var challengerWonEvent = new DuelEventDto
                {
                    DuelId = duelState.Id,
                    Sequence = sequence,
                    CharacterId = characterId,
                    EventType = Enums.EventType.DamageDealt,
                    TimeWhenNextActionOfTypeAvailable = DateTime.Now.AddSeconds(this.damageCooldownInSeconds),
                    Damage = duelState.Challenger.Damage
                };

                duelDto.Events.Add(challengerWonEvent);

                await PersistDuelInfo(duelState);
                return challengerWonEvent;
            }

            if (duelState.Defender.Id == characterId && (duelState.Challenger.Health - damageDealt) <= 0)
            {
                duelState.Challenger.Health = 0;
                duelState.DuelOutcome = Enums.DuelOutcome.DefenderWon;

                var defenderWonEvent = new DuelEventDto
                {
                    DuelId = duelState.Id,
                    Sequence = sequence,
                    CharacterId = characterId,
                    EventType = Enums.EventType.DamageDealt,
                    TimeWhenNextActionOfTypeAvailable = DateTime.Now.AddSeconds(this.damageCooldownInSeconds),
                    Damage = duelState.Defender.Damage
                };

                duelDto.Events.Add(defenderWonEvent);

                await PersistDuelInfo(duelState);
                return defenderWonEvent;
            }

            var @event = new DuelEventDto
            {
                DuelId = duelState.Id,
                Sequence = sequence,
                CharacterId = characterId,
                EventType = Enums.EventType.DamageDealt,
                TimeWhenNextActionOfTypeAvailable = DateTime.Now.AddSeconds(this.damageCooldownInSeconds),
                Damage = damageDealt
            };

            await AddDuelToCache(duelDto);

            duelDto.Events.Add(@event);

            return @event;
        }

        public async Task<DuelEventDto> HealSelf(Guid duelId, Guid characterId)
        {
            var duelDto = await GetDuelFromCacheOrDb(duelId, false) ??
                throw new Status400Exception($"Duel with id = {duelId} not found or is finished.");
            
            var duelState = await ComputeStateForDuel(duelId);
            var healingDone = characterId == duelState.Challenger.Id ? duelState.Challenger.Healing : duelState.Defender.Healing;

            if (IsDuelADraw(duelState))
            {
                duelState.DuelOutcome = Enums.DuelOutcome.Draw;

                await PersistDuelInfo(duelState);

                throw new Status400Exception($"The time has run out, the duel ended with a draw.");
            }

            if (characterId == duelState.Challenger.Id && duelState.Challenger.DamageCooldown > DateTime.Now)
            {
                throw new Status400Exception($"Damage for character with id = {characterId} is still on cooldown.");
            }

            if (characterId == duelState.Defender.Id && duelState.Defender.DamageCooldown > DateTime.Now)
            {
                throw new Status400Exception($"Damage for character with id = {characterId} is still on cooldown.");
            }

            var sequence = duelDto.Events.Max(x => x.Sequence) + 1;

            if (duelState.Challenger.Id == characterId && (duelState.Challenger.Health + healingDone) >= duelState.Challenger.InitialHealth)
            {
                duelState.Challenger.Health = duelState.Challenger.InitialHealth;

                var challengerHealedEvent = new DuelEventDto
                {
                    DuelId = duelState.Id,
                    Sequence = sequence,
                    CharacterId = characterId,
                    EventType = Enums.EventType.HealedSelf,
                    TimeWhenNextActionOfTypeAvailable = DateTime.Now.AddSeconds(this.healingCooldownInSeconds),
                    Healing = duelState.Challenger.InitialHealth - duelState.Challenger.Health
                };

                duelDto.Events.Add(challengerHealedEvent);

                await AddDuelToCache(duelDto);

                return challengerHealedEvent;
            }

            if (duelState.Defender.Id == characterId && (duelState.Defender.Health + healingDone) >= duelState.Defender.InitialHealth)
            {
                duelState.Defender.Health = duelState.Defender.InitialHealth;

                var defenderHealedEvent = new DuelEventDto
                {
                    DuelId = duelState.Id,
                    Sequence = sequence,
                    CharacterId = characterId,
                    EventType = Enums.EventType.HealedSelf,
                    TimeWhenNextActionOfTypeAvailable = DateTime.Now.AddSeconds(this.healingCooldownInSeconds),
                    Healing = duelState.Defender.InitialHealth - duelState.Challenger.Health
                };
                duelDto.Events.Add(defenderHealedEvent);

                await AddDuelToCache(duelDto);

                return defenderHealedEvent;
            }

            var @event = new DuelEventDto
            {
                DuelId = duelState.Id,
                Sequence = sequence,
                CharacterId = characterId,
                EventType = Enums.EventType.HealedSelf,
                TimeWhenNextActionOfTypeAvailable = DateTime.Now.AddSeconds(this.healingCooldownInSeconds),
                Healing = healingDone
            };

            duelDto.Events.Add(@event);

            await AddDuelToCache(duelDto);

            return @event;
        }

        public async Task<DuelState> ComputeStateForDuel(Guid duelId, bool checkDb = false)
        {
            var state = new DuelState()
            {
                Id = duelId,
                Challenger = new CharacterInfo(),
                Defender = new CharacterInfo(),
            };

            var duelDto = await GetDuelFromCacheOrDb(duelId, checkDb);

            if (duelDto != null)
            {
                return this.ComputeStateFromEvents(state, duelDto.Events);
            }

            return state;
        }

        public DuelState ComputeStateFromEvents(DuelState state, IEnumerable<DuelEventDto> events)
        {
            var ordered = events.OrderBy(x => x.Sequence);

            foreach (var @event in ordered)
            {
                switch (@event.EventType)
                {
                    case Enums.EventType.Init:
                        if (@event.Sequence == 0)
                        {
                            state.Challenger.Id = @event.CharacterId;
                            state.Challenger.Health = @event.InitialHealth;
                            state.Challenger.InitialHealth = @event.InitialHealth;
                            state.Challenger.Mana = @event.InitialMana;
                            state.Challenger.InitialMana = @event.InitialMana;
                            state.Challenger.Damage = @event.Damage;
                            state.Challenger.Healing = @event.Healing;
                            break;
                        }

                        state.DuelStart = @event.TimeWhenNextActionOfTypeAvailable;
                        state.Defender.Id = @event.CharacterId;
                        state.Defender.Health = @event.InitialHealth;
                        state.Defender.InitialHealth = @event.InitialHealth;
                        state.Defender.Mana = @event.InitialMana;
                        state.Defender.InitialMana = @event.InitialMana;
                        state.Defender.Damage = @event.Damage;
                        state.Defender.Healing = @event.Healing;
                        break;

                    case Enums.EventType.DamageDealt:
                        if (@event.CharacterId == state.Challenger.Id)
                        {
                            state.Defender.Health -= @event.Damage;
                            state.Challenger.DamageCooldown = @event.TimeWhenNextActionOfTypeAvailable;
                            break;
                        }

                        state.Challenger.Health -= @event.Damage;
                        state.Defender.DamageCooldown = @event.TimeWhenNextActionOfTypeAvailable;
                        break;

                    case Enums.EventType.HealedSelf:
                        if (@event.CharacterId == state.Challenger.Id)
                        {
                            state.Challenger.Health += @event.Healing;
                            break;
                        }

                        state.Defender.Health += @event.Healing;
                        break;

                    case Enums.EventType.SpellCast:
                        if (@event.CharacterId == state.Challenger.Id)
                        {
                            state.Challenger.Health += @event.Healing;
                            state.Challenger.Mana -= @event.ManaSpent;
                            state.Defender.Health -= @event.Damage;
                            break;
                        }

                        state.Defender.Health += @event.Healing;
                        state.Defender.Mana -= @event.ManaSpent;
                        state.Challenger.Health -= @event.Damage;
                        break;
                }
            }

            return state;
        }

        public async Task CheckIfDuelExists(DuelRequestDto duelRequestDto)
        {
            var existingDuel = await dbContext.Duels
                    .FirstOrDefaultAsync(x => x.DuelOutcome == Enums.DuelOutcome.Ongoing &&
                        (x.ChallengerId == duelRequestDto.ChallengerId || x.ChallengerId == duelRequestDto.DefenderId ||
                        x.DefenderId == duelRequestDto.ChallengerId || x.DefenderId == duelRequestDto.DefenderId));

            if (existingDuel != null)
            {
                logger.LogError(message: "Ongoing duel with participants {challengerId} or {defenderId} already exists. DuelId = {duelId}",
                    duelRequestDto.ChallengerId, duelRequestDto.DefenderId, existingDuel.Id);

                throw new Status400Exception($"Ongoing duel with participants {duelRequestDto.DefenderId} or {duelRequestDto.ChallengerId} already exists.");
            }
        }

        public async Task<DuelDto> GetDuelFromCacheOrDb(Guid id, bool checkDb = true)
        {
            var cachedData = await redisDatabase.StringGetAsync($"{nameof(DuelDto)}:{id}");

            if (cachedData.HasValue)
            {
                return JsonConvert.DeserializeObject<DuelDto>(cachedData!)!;
            }
            else if (checkDb)
            {
                var data = await dbContext.Duels.FirstOrDefaultAsync(x => x.Id == id) ??
                    throw new Status400Exception("Duel with id doesn't exist.");

                var mapped = this.mapper.Map<DuelDto>(data);

                await redisDatabase.StringSetAsync($"{nameof(DuelDto)}:{id}",
                    JsonConvert.SerializeObject(mapped), expiry: TimeSpan.FromSeconds(this.maximumDuelDurationInSeconds));

                return mapped;
            }

            return null!;
        }

        private async Task PersistDuelInfo(DuelState duelState)
        {
            var duelDto = await GetDuelFromCacheOrDb(duelState.Id);

            var mapped = mapper.Map<Duel>(duelDto);

            this.dbContext.Duels.Update(mapped);
            await this.dbContext.SaveChangesAsync();

            if (duelDto.DuelOutcome > Enums.DuelOutcome.Ongoing)
            {
                RemoveDuelFromCache(duelDto);
            }
        }

        private async Task<IEnumerable<DuelSpellDto>> GetSpellsFromCacheOrDbForCharacter(Guid ownerId)
        {
            var cachedData = await redisDatabase.StringGetAsync($"{nameof(DuelSpellDto)}:{ownerId}");

            if (cachedData.HasValue)
            {
                return JsonConvert.DeserializeObject<IEnumerable<DuelSpellDto>>(cachedData!)!;
            }
            else
            {
                var data = await dbContext.Spells.Where(x => x.OwnerId == ownerId).ToListAsync();
                var mapped = this.mapper.Map<List<DuelSpellDto>>(data);

                await redisDatabase.StringSetAsync($"{nameof(DuelSpellDto)}:{ownerId}",
                    JsonConvert.SerializeObject(mapped), expiry: TimeSpan.FromSeconds(this.maximumDuelDurationInSeconds));

                return mapped;
            }
        }

        public async Task<bool> AddDuelToCache(DuelDto duelDto)
        {
            return await redisDatabase.StringSetAsync($"{nameof(DuelDto)}:{duelDto.Id}",
                JsonConvert.SerializeObject(duelDto), expiry: TimeSpan.FromSeconds(this.maximumDuelDurationInSeconds));
        }

        private bool RemoveDuelFromCache(DuelDto duelDto)
        {
            return redisDatabase.KeyDelete($"{nameof(DuelDto)}:{duelDto.Id}");
        }

        private bool IsDuelADraw(DuelState duelDto)
        {
            return duelDto.DuelStart.AddMinutes(this.maximumDuelDurationInSeconds) <= DateTime.Now;
        }
    }
}
