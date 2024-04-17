using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pipelines.Sockets.Unofficial.Buffers;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WoG.Combat.Services.Api.Data;
using WoG.Combat.Services.Api.Models;
using WoG.Combat.Services.Api.Models.Dtos;
using WoG.Combat.Services.Api.Rpc;
using WoG.Combat.Services.Api.Services;
using WoG.Core.RabbitMqCommunication.Requests;
using WoG.Core.RabbitMqCommunication.Responses;

namespace WoG.Combat.Services.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CombatApiController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<CombatApiController> logger;

        private readonly IMapper mapper;
        private readonly CombatService combatService;

        private readonly int damageCooldownInSeconds;
        private readonly int healingCooldownInSeconds;
        private readonly int maximumDuelDurationInSeconds;

        private readonly IDatabase redisDatabase;

        public CombatApiController(ApplicationDbContext dbContext, CombatService combatService, IConnectionMultiplexer redisConnectionMultiplexer,
            ILogger<CombatApiController> Logger, IMapper mapper, IConfiguration appSettings)
        {
            this.damageCooldownInSeconds = appSettings.GetValue<int>("CombatProperties:DamageCooldownInSeconds");
            this.healingCooldownInSeconds = appSettings.GetValue<int>("CombatProperties:HealingCooldownInSeconds");
            this.maximumDuelDurationInSeconds = appSettings.GetValue<int>("CombatProperties:MaximumDuelDurationInSeconds");

            this.redisDatabase = redisConnectionMultiplexer.GetDatabase();
            this.dbContext = dbContext;
            this.combatService = combatService;
            this.logger = Logger;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ObjectResult> Get()
        {
            try
            {
                var duels = await dbContext.Duels.ToListAsync();
                var result = mapper.Map<IEnumerable<DuelDto>>(duels);

                return this.StatusCode(StatusCodes.Status200OK, result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occured when getting duels.");
                return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ObjectResult> Post([FromBody] DuelRequestDto duelRequestDto)
        {
            try
            {
                var existingDuel = dbContext.Duels
                    .FirstOrDefault(x => x.DuelOutcome == Enums.DuelOutcome.Ongoing && 
                        (x.ChallengerId == duelRequestDto.ChallengerId || x.ChallengerId == duelRequestDto.DefenderId ||
                        x.DefenderId == duelRequestDto.ChallengerId || x.DefenderId == duelRequestDto.DefenderId));

                if (existingDuel != null)
                {
                    logger.LogError(message: "Ongoing duel with participants {challengerId} or {defenderId} already exists. DuelId = {duelId}",
                        duelRequestDto.ChallengerId, duelRequestDto.DefenderId, existingDuel.Id);

                    return this.StatusCode(StatusCodes.Status400BadRequest,
                        $"Ongoing duel with participants {duelRequestDto.DefenderId} or {duelRequestDto.ChallengerId} already exists.");
                }

                var queueDuelResponse = await combatService.QueueDuel(duelRequestDto.ChallengerId, duelRequestDto.DefenderId);

                var duel = dbContext.Duels.First(x => x.Id == queueDuelResponse.DuelId);
                var duelDto = mapper.Map<DuelDto>(duel);

                await AddDuelToCache(duelDto);

                return this.StatusCode(StatusCodes.Status200OK, duelDto);
            }
            catch(InvalidOperationException)
            {
                logger.LogError(message: "Defender with Id = {defenderId} doesn't exist in system.", duelRequestDto.DefenderId);
                
                return this.StatusCode(StatusCodes.Status400BadRequest, 
                    $"Defender with Id = {duelRequestDto.DefenderId} doesn't exist in system.");

            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "Error when trying to challenge defender");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet]
        [Route("{id:Guid}")]
        public async Task<ObjectResult> Get(Guid id)
        {
            try
            {
                var response = await GetDuelFromCacheOrDb(id);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (InvalidOperationException)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, "Get :: Duel with id doesn't exist.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occured when getting duel information.");
                return this.StatusCode(StatusCodes.Status500InternalServerError, new object());
            }
        }

        [HttpPost]
        [Route("DealDamage")]
        public async Task<ObjectResult> DealDamage(Guid duelId, Guid characterId)
        {
            var duelDto = await GetDuelFromCacheOrDb(duelId, false);

            if (duelDto == null)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, "DealDamage :: Duel with id doesn't exist or is already finished.");
            }

            var duelState = await ComputeStateForDuelId(duelId);
            var damageDealt = characterId == duelState.Challenger.Id ? duelState.Challenger.Damage : duelState.Defender.Damage;

            if (IsDuelADraw(duelState))
            {
                duelState.DuelOutcome = Enums.DuelOutcome.Draw;

                await PersistDuelInfo(duelState);

                return this.StatusCode(StatusCodes.Status400BadRequest, $"The time has run out, the duel ended with a draw.");
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
                return this.StatusCode(StatusCodes.Status200OK, challengerWonEvent);
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
                return this.StatusCode(StatusCodes.Status200OK, defenderWonEvent);
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

            return this.StatusCode(StatusCodes.Status200OK, @event);
        }
        
        [HttpPost]
        [Route("CastSpell")]
        public async Task<ObjectResult> CastSpell(Guid duelId, Guid characterId, Guid spellId)
        {
            var duelDto = await GetDuelFromCacheOrDb(duelId, false);

            if (duelDto == null)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, "CastSpell :: Duel with id doesn't exist or is already finished.");
            }

            var spells = await GetSpellsFromCacheOrDbForCharacter(characterId);
            var spell = spells.FirstOrDefault(x => x.Id == spellId);

            if(spell == null)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest, $"CastSpell :: Spell with Id = {spellId} isn't known by character with id = {characterId}.");
            }

            var duelState = await ComputeStateForDuelId(duelId);

            if (IsDuelADraw(duelState))
            {
                duelState.DuelOutcome = Enums.DuelOutcome.Draw;

                await PersistDuelInfo(duelState);

                return this.StatusCode(StatusCodes.Status400BadRequest, $"The time has run out, the duel ended with a draw.");
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
                return this.StatusCode(StatusCodes.Status200OK, challengerWonEvent);
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
                return this.StatusCode(StatusCodes.Status200OK, defenderWonEvent);
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

            return this.StatusCode(StatusCodes.Status200OK, @event);
        }

        [HttpPost]
        [Route("HealSelf")]
        public async Task<ObjectResult> HealSelf(Guid duelId, Guid characterId)
        {
            var duelDto = await GetDuelFromCacheOrDb(duelId, false);

            if (duelDto == null)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest,$"Duel with id = {duelId} not found or is finished.");
            }

            var duelState = await ComputeStateForDuelId(duelId);
            var healingDone = characterId == duelState.Challenger.Id ? duelState.Challenger.Healing : duelState.Defender.Healing;

            if (IsDuelADraw(duelState))
            {
                duelState.DuelOutcome = Enums.DuelOutcome.Draw;
                
                await PersistDuelInfo(duelState);

                return this.StatusCode(StatusCodes.Status400BadRequest,$"The time has run out, the duel ended with a draw.");
            }

            if (characterId == duelState.Challenger.Id && duelState.Challenger.DamageCooldown > DateTime.Now)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest,$"Damage for character with id = {characterId} is still on cooldown.");
            }

            if (characterId == duelState.Defender.Id && duelState.Defender.DamageCooldown > DateTime.Now)
            {
                return this.StatusCode(StatusCodes.Status400BadRequest,$"Damage for character with id = {characterId} is still on cooldown.");
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

                return this.StatusCode(StatusCodes.Status200OK, challengerHealedEvent);
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

                return this.StatusCode(StatusCodes.Status200OK, defenderHealedEvent);
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

            return this.StatusCode(StatusCodes.Status200OK, @event);
        }

        [HttpGet]
        [Route("ComputeStateForDuel/{duelId:Guid}")]
        public async Task<ObjectResult> ComputeStateForDuel(Guid duelId)
        {
            try
            {
                var state = await this.ComputeStateForDuelId(duelId, true);
                return this.StatusCode(StatusCodes.Status200OK, state);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occured when getting base item.");
                return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private async Task<DuelState> ComputeStateForDuelId(Guid duelId, bool checkDb = false)
        {
            var state = new DuelState()
            {
                Id = duelId,
                Challenger = new Models.Dtos.CharacterInfo(),
                Defender = new Models.Dtos.CharacterInfo(),
            };

            var duelDto = await GetDuelFromCacheOrDb(duelId, checkDb);

            if (duelDto != null)
            {
                return ComputeStateFromEvents(state, duelDto.Events);
            }

            return state;
        }

        private static DuelState ComputeStateFromEvents(DuelState state, IEnumerable<DuelEventDto> events)
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
                return JsonConvert.DeserializeObject<IEnumerable<DuelSpellDto>>(cachedData);
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

        private async Task<DuelDto> GetDuelFromCacheOrDb(Guid id, bool checkDb = true)
        {
            var cachedData = await redisDatabase.StringGetAsync($"{nameof(DuelDto)}:{id}");

            if (cachedData.HasValue)
            {
                return JsonConvert.DeserializeObject<DuelDto>(cachedData);
            }
            else if(checkDb)
            {
                var data = await dbContext.Duels.FirstAsync(x => x.Id == id);
                var mapped = this.mapper.Map<DuelDto>(data);

                await redisDatabase.StringSetAsync($"{nameof(DuelDto)}:{id}",
                    JsonConvert.SerializeObject(mapped), expiry: TimeSpan.FromSeconds(this.maximumDuelDurationInSeconds));

                return mapped;
            }

            return null;
        }

        private async Task<bool> AddDuelToCache(DuelDto duelDto)
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
