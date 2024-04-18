using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoG.Combat.Services.Api.Data;
using WoG.Combat.Services.Api.Exceptions;
using WoG.Combat.Services.Api.Models.Dtos;
using WoG.Combat.Services.Api.Repositories.Interfaces;
using WoG.Combat.Services.Api.Services;

namespace WoG.Combat.Services.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CombatApiController(ApplicationDbContext dbContext, CombatService combatService, ICombatRepository combatRepository,
        ILogger<CombatApiController> Logger, IMapper mapper) : ControllerBase
    {
        private readonly ApplicationDbContext dbContext = dbContext;
        private readonly ILogger<CombatApiController> logger = Logger;

        private readonly IMapper mapper = mapper;
        private readonly CombatService combatService = combatService;
        private readonly ICombatRepository combatRepository = combatRepository;

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
                await this.combatRepository.CheckIfDuelExists(duelRequestDto);

                var queueDuelResponse = await combatService.QueueDuel(duelRequestDto.ChallengerId, duelRequestDto.DefenderId);

                var duel = await dbContext.Duels.FirstAsync(x => x.Id == queueDuelResponse.DuelId);
                var duelDto = mapper.Map<DuelDto>(duel);

                await this.combatRepository.AddDuelToCache(duelDto);

                return this.StatusCode(StatusCodes.Status200OK, duelDto);
            }
            catch(Status400Exception s400e)
            {
                logger.LogError(s400e.Message);
                return this.StatusCode(StatusCodes.Status400BadRequest, s400e.Message);

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
                var response = await this.combatRepository.GetDuelFromCacheOrDb(id);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Status400Exception s400e)
            {
                logger.LogError(s400e.Message);
                return this.StatusCode(StatusCodes.Status400BadRequest, s400e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error occured when getting duel information.");
                return this.StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost]
        [Route("DealDamage")]
        public async Task<ObjectResult> DealDamage(Guid duelId, Guid characterId)
        {
            try
            {
                var @event = await this.combatRepository.DealDamage(duelId, characterId);
                return this.StatusCode(StatusCodes.Status200OK, @event);
            }
            catch (Status400Exception s400e)
            {
                logger.LogError(s400e.Message);
                return this.StatusCode(StatusCodes.Status400BadRequest, s400e.Message);
            }
            catch (Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
        
        [HttpPost]
        [Route("CastSpell")]
        public async Task<ObjectResult> CastSpell(Guid duelId, Guid characterId, Guid spellId)
        {
            try
            {
                var @event = await this.combatRepository.CastSpell(duelId, characterId, spellId);
                return this.StatusCode(StatusCodes.Status200OK, @event);
            }
            catch (Status400Exception s400e)
            {
                logger.LogError(s400e.Message);
                return this.StatusCode(StatusCodes.Status400BadRequest, s400e.Message);
            }
            catch (Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost]
        [Route("HealSelf")]
        public async Task<ObjectResult> HealSelf(Guid duelId, Guid characterId)
        {
            try
            {
                var @event = await this.combatRepository.HealSelf(duelId, characterId);
                return this.StatusCode(StatusCodes.Status200OK, @event);
            }
            catch(Status400Exception s400e)
            {
                logger.LogError(s400e.Message);
                return this.StatusCode(StatusCodes.Status400BadRequest, s400e.Message);
            }
            catch(Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet]
        [Route("ComputeStateForDuel/{duelId:Guid}")]
        public async Task<ObjectResult> ComputeStateForDuel(Guid duelId)
        {
            try
            {
                var state = await this.combatRepository.ComputeStateForDuel(duelId, true);
                return this.StatusCode(StatusCodes.Status200OK, state);
            }
            catch(Status400Exception s400e)
            {
                logger.LogError(s400e.Message);
                return this.StatusCode(StatusCodes.Status400BadRequest, s400e.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occured when getting base item.");
                return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
